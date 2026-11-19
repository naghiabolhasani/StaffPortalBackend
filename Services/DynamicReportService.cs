using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Services;

public class DynamicReportService : IDynamicReportService
{
    private readonly IConfiguration _configuration;
    private readonly IPermissionService _permissionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DynamicReportService> _logger;

    private const int MaxPageSize = 500;
    private const int MaxRowsHardLimit = 10000;

    private static readonly string[] DangerousKeywords =
    {
        ";", "--", "/*", "*/", "DROP", "DELETE", "UPDATE", "INSERT",
        "EXEC", "EXECUTE", "TRUNCATE", "ALTER", "CREATE", "MERGE",
        "xp_", "sp_", "GRANT", "REVOKE"
    };

    public DynamicReportService(
        IConfiguration configuration,
        IPermissionService permissionService,
        IMemoryCache cache,
        ILogger<DynamicReportService> logger)
    {
        _configuration = configuration;
        _permissionService = permissionService;
        _cache = cache;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  API 1: لیست گزارش‌های مجاز کاربر
    // ═══════════════════════════════════════════════════════════
    public async Task<List<ReportCategoryDto>> GetReportsForUserAsync(int userId)
    {
        var connectionString = GetConnectionString();

        // 1. دریافت تمام مجوزهای کاربر
        var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);
        var permissionSet = new HashSet<string>(
            userPermissions
                .Where(p => !string.IsNullOrEmpty(p.Action))
                .Select(p => $"{p.Resource}.{p.Action}"),
            StringComparer.OrdinalIgnoreCase);

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // 2. دریافت تمام گزارش‌های فعال
        var sql = @"
            SELECT 
                r.Id,
                r.Title,
                r.Dsc,
                r.CategoryId,
                r.PermissionRef,
                c.Id AS CatId,
                c.CategoryName,
                c.ParentRef,
                c.DisplayOrder AS CatDisplayOrder
            FROM Rpt.Reports r WITH (NOLOCK)
            INNER JOIN Rpt.ReportCategories c WITH (NOLOCK) 
                ON c.Id = r.CategoryId
            WHERE r.IsActive = 1 
              AND c.IsActive = 1
            ORDER BY c.DisplayOrder, c.Id, r.Title";

        var rows = (await conn.QueryAsync<dynamic>(sql)).ToList();

        // 3. فیلتر بر اساس مجوز
        var allowedReports = rows.Where(r =>
        {
            string? permRef = r.PermissionRef;
            if (string.IsNullOrEmpty(permRef))
                return true; // بدون مجوز = برای همه

            return permissionSet.Contains(permRef);
        }).ToList();

        // 4. گروه‌بندی بر اساس دسته
        var categories = new Dictionary<int, ReportCategoryDto>();

        foreach (var r in allowedReports)
        {
            int catId = (int)r.CatId;

            if (!categories.ContainsKey(catId))
            {
                categories[catId] = new ReportCategoryDto
                {
                    Id = catId,
                    CategoryName = (string)r.CategoryName,
                    ParentRef = (int?)r.ParentRef,
                    DisplayOrder = (int)r.CatDisplayOrder
                };
            }

            categories[catId].Reports.Add(new DynamicReportListItemDto
            {
                Id = (int)r.Id,
                Title = (string)r.Title,
                Dsc = (string?)r.Dsc,
                CategoryId = (int?)r.CategoryId,
                PermissionRef = (string?)r.PermissionRef
            });
        }

        _logger.LogInformation(
            "User {UserId} has access to {ReportCount} reports in {CategoryCount} categories",
            userId, allowedReports.Count, categories.Count);

        return categories.Values.OrderBy(c => c.DisplayOrder).ToList();
    }

    // ═══════════════════════════════════════════════════════════
    //  API 2: اطلاعات کامل گزارش (Metadata)
    // ═══════════════════════════════════════════════════════════
    public async Task<DynamicReportInfoResponse> GetReportInfoAsync(int reportId)
    {
        var connectionString = GetConnectionString();
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // 1. اطلاعات اصلی گزارش
        var reportSql = @"
            SELECT 
                Id, Title, Dsc, CategoryId, BaseQuery, DefaultOrderBy,
                AllowUserSort, AllowUserFilter, AllowPaging,
                DefaultPageSize, MaxPageSize, Version
            FROM Rpt.Reports WITH (NOLOCK)
            WHERE Id = @ReportId AND IsActive = 1";

        var report = await conn.QueryFirstOrDefaultAsync<ReportMetaDto>(reportSql, new { ReportId = reportId });

        if (report == null)
            throw new ArgumentException($"گزارش با شناسه {reportId} یافت نشد یا غیرفعال است.");

        // 2. پارامترها
        var paramsSql = @"
            SELECT 
                Id, ParamName, DisplayName, DataType, ParamKind,
                SystemSource, WhereExpression, IsVisible, DefaultValue,
                ControlType, LookupId, ValidationRegex, MinValue, MaxValue, DisplayOrder
            FROM Rpt.ReportParameters WITH (NOLOCK)
            WHERE ReportRef = @ReportId
            ORDER BY DisplayOrder";

        var parameters = (await conn.QueryAsync<ReportParameterDto>(paramsSql, new { ReportId = reportId }))
            .ToList();

        foreach (var p in parameters)
        {
            p.ParamNameWithoutAt = p.ParamName.StartsWith("@") ? p.ParamName.Substring(1) : p.ParamName;
        }

        // 3. فیلترهای اختیاری
        var filtersSql = @"
            SELECT 
                Id, FieldName, DisplayName, DataType, AllowedOps, DefaultOp,
                ControlType, LookupRef, IsMultiSelect, DisplayOrder
            FROM Rpt.ReportOptionalFilters WITH (NOLOCK)
            WHERE ReportRef = @ReportId AND IsActive = 1
            ORDER BY DisplayOrder";

        var filters = (await conn.QueryAsync<ReportOptionalFilterDto>(filtersSql, new { ReportId = reportId }))
            .ToList();

        // 4. ستون‌های قابل مرتب‌سازی
        var sortableSql = @"
            SELECT 
                SortId, FieldName, DisplayName, IsDefault,
                DefaultDirection, DisplayOrder
            FROM Rpt.ReportSortableColumns WITH (NOLOCK)
            WHERE ReportRef = @ReportId
            ORDER BY DisplayOrder";

        var sortableColumns = (await conn.QueryAsync<ReportSortableColumnDto>(sortableSql, new { ReportId = reportId }))
            .ToList();

        // 5. ستون‌های خروجی
        var columnsSql = @"
            SELECT 
                Id, FieldName, DisplayName, DataType, Format,
                Align, Width, IsVisible, AggregateFunc, DisplayOrder
            FROM Rpt.ReportColumns WITH (NOLOCK)
            WHERE ReportRef = @ReportId
            ORDER BY DisplayOrder";

        var columns = (await conn.QueryAsync<ReportColumnDto>(columnsSql, new { ReportId = reportId }))
            .ToList();

        // 6. دریافت Lookups (کمبوباکس‌ها)
        var lookupIds = parameters.Where(p => p.LookupId.HasValue).Select(p => p.LookupId!.Value)
            .Concat(filters.Where(f => f.LookupRef.HasValue).Select(f => f.LookupRef!.Value))
            .Distinct()
            .ToList();

        var lookups = new Dictionary<int, List<LookupItemDto>>();

        if (lookupIds.Count > 0)
        {
            var lookupsSql = @"
                SELECT Id, DisplayName, QueryText, CacheMinutes
                FROM Rpt.Lookups WITH (NOLOCK)
                WHERE Id IN @Ids AND IsActive = 1";

            var lookupDefs = (await conn.QueryAsync<dynamic>(lookupsSql, new { Ids = lookupIds })).ToList();

            foreach (var ld in lookupDefs)
            {
                int lookupId = (int)ld.Id;
                string displayName = (string)ld.DisplayName;
                string queryText = (string)ld.QueryText;
                int cacheMinutes = (int)ld.CacheMinutes;

                var cacheKey = $"Lookup_{lookupId}_v1";
                if (_cache.TryGetValue(cacheKey, out List<LookupItemDto>? cached) && cached != null)
                {
                    lookups[lookupId] = cached;
                    continue;
                }

                // اجرای امن کوئری Lookup
                ValidateSqlQuery(queryText);

                try
                {
                    var items = (await conn.QueryAsync<LookupItemDto>(queryText)).ToList();
                    lookups[lookupId] = items;

                    if (cacheMinutes > 0)
                    {
                        _cache.Set(cacheKey, items, TimeSpan.FromMinutes(cacheMinutes));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing lookup {LookupId} ({DisplayName})", lookupId, displayName);
                    lookups[lookupId] = new List<LookupItemDto>();
                }
            }
        }

        return new DynamicReportInfoResponse
        {
            Report = report,
            Parameters = parameters,
            Filters = filters,
            SortableColumns = sortableColumns,
            Columns = columns,
            Lookups = lookups
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  API 3: اجرای گزارش
    // ═══════════════════════════════════════════════════════════
    public async Task<DynamicReportRunResponse> RunReportAsync(
        DynamicReportRunRequest request,
        int currentUserId)
    {
        // 1. دریافت metadata
        var info = await GetReportInfoAsync(request.ReportId);

        // 2. اعتبارسنجی پیجینگ
        int pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        int pageSize = request.PageSize < 1 ? info.Report.DefaultPageSize : request.PageSize;
        if (pageSize > info.Report.MaxPageSize) pageSize = info.Report.MaxPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        // 3. پارامترها
        var parameters = new DynamicParameters();

        // 3-1. پارامترهای اجباری
        var requiredWhereClauses = new List<string>();

        foreach (var p in info.Parameters)
        {
            if (p.ParamKind == 1) // Required
            {
                if (!request.RequiredParams.TryGetValue(p.ParamNameWithoutAt, out var rawValue) || rawValue == null)
                {
                    throw new ArgumentException($"پارامتر «{p.DisplayName}» اجباری است.");
                }

                // اعتبارسنجی Regex
                if (!string.IsNullOrEmpty(p.ValidationRegex))
                {
                    var strVal = rawValue.ToString() ?? "";
                    if (!Regex.IsMatch(strVal, p.ValidationRegex))
                        throw new ArgumentException($"مقدار «{p.DisplayName}» نامعتبر است.");
                }

                var sqlValue = ConvertToSqlValue(rawValue, p.DataType);
                parameters.Add(p.ParamNameWithoutAt, sqlValue);
                requiredWhereClauses.Add($"({p.WhereExpression})");
            }
            else if (p.ParamKind == 2) // System
            {
                var sysValue = GetSystemValue(p.SystemSource, currentUserId);
                parameters.Add(p.ParamNameWithoutAt, sysValue);
                requiredWhereClauses.Add($"({p.WhereExpression})");
            }
        }

        // 3-2. فیلترهای اختیاری
        var optionalWhereClauses = new List<string>();
        int filterIdx = 0;

        foreach (var filter in request.Filters)
        {
            // Whitelist
            var allowed = info.Filters.FirstOrDefault(f =>
                f.FieldName.Equals(filter.Field, StringComparison.OrdinalIgnoreCase));

            if (allowed == null)
            {
                _logger.LogWarning("Filter field {Field} is not whitelisted for report {ReportId}",
                    filter.Field, request.ReportId);
                continue;
            }

            // بررسی عملگر مجاز
            var ops = allowed.AllowedOps
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.ToUpperInvariant())
                .ToHashSet();

            var op = (filter.Op ?? "").ToUpperInvariant();
            if (!ops.Contains(op))
            {
                throw new ArgumentException($"عملگر «{filter.Op}» برای فیلتر «{allowed.DisplayName}» مجاز نیست.");
            }

            // ساخت WHERE با استفاده از FieldName از DB (نه از ورودی کاربر)
            var fieldName = allowed.FieldName;
            var paramBase = $"f{filterIdx++}";

            switch (op)
            {
                case "=":
                case "<>":
                case ">":
                case "<":
                case ">=":
                case "<=":
                    parameters.Add(paramBase, ConvertToSqlValue(filter.Value1, allowed.DataType));
                    optionalWhereClauses.Add($"({fieldName} {op} @{paramBase})");
                    break;

                case "LIKE":
                    parameters.Add(paramBase, filter.Value1 ?? "");
                    optionalWhereClauses.Add($"({fieldName} LIKE '%' + @{paramBase} + '%')");
                    break;

                case "BETWEEN":
                    parameters.Add($"{paramBase}_1", ConvertToSqlValue(filter.Value1, allowed.DataType));
                    parameters.Add($"{paramBase}_2", ConvertToSqlValue(filter.Value2, allowed.DataType));
                    optionalWhereClauses.Add($"({fieldName} BETWEEN @{paramBase}_1 AND @{paramBase}_2)");
                    break;

                case "IN":
                    if (filter.Values == null || filter.Values.Count == 0)
                        continue;

                    if (!allowed.IsMultiSelect)
                        throw new ArgumentException($"فیلتر «{allowed.DisplayName}» از چند مقدار پشتیبانی نمی‌کند.");

                    var inParams = new List<string>();
                    int vIdx = 0;
                    foreach (var v in filter.Values)
                    {
                        var vp = $"{paramBase}_{vIdx++}";
                        parameters.Add(vp, ConvertToSqlValue(v, allowed.DataType));
                        inParams.Add($"@{vp}");
                    }
                    optionalWhereClauses.Add($"({fieldName} IN ({string.Join(",", inParams)}))");
                    break;

                default:
                    throw new ArgumentException($"عملگر «{op}» پشتیبانی نمی‌شود.");
            }
        }

        // 4. ORDER BY
        string? orderBy = BuildOrderBy(info, request.Sorts);

        // 5. ساخت کوئری نهایی
        var baseQuery = info.Report.BaseQuery.TrimEnd().TrimEnd(';');

        var requiredWhere = requiredWhereClauses.Count > 0
            ? " AND " + string.Join(" AND ", requiredWhereClauses)
            : "";

        var optionalWhere = optionalWhereClauses.Count > 0
            ? " AND " + string.Join(" AND ", optionalWhereClauses)
            : "";

        baseQuery = baseQuery
            .Replace("/* {{REQUIRED_WHERE}} */", requiredWhere, StringComparison.OrdinalIgnoreCase)
            .Replace("/* {{OPTIONAL_WHERE}} */", optionalWhere, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(orderBy))
            orderBy = "1";

        var pagedSql = $@"
            SELECT *
            FROM (
                {baseQuery}
            ) AS subq
            ORDER BY {orderBy}
            OFFSET {(pageNumber - 1) * pageSize} ROWS
            FETCH NEXT {pageSize} ROWS ONLY";

        // 6. اجرا
        using var conn = new SqlConnection(GetConnectionString());
        await conn.OpenAsync();

        var rows = (await conn.QueryAsync(pagedSql, parameters))
            .Select(r => (IDictionary<string, object?>)r)
            .Select(d => new Dictionary<string, object?>(d))
            .ToList();

        // 7. محاسبه Aggregates
        var aggregates = await CalculateAggregatesAsync(conn, info, baseQuery, parameters);

        _logger.LogInformation(
            "Report {ReportId} executed by user {UserId}: {RowCount} rows returned (page {Page})",
            request.ReportId, currentUserId, rows.Count, pageNumber);

        return new DynamicReportRunResponse
        {
            Rows = rows,
            Columns = info.Columns,
            Aggregates = aggregates
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Helper: ساخت ORDER BY
    // ═══════════════════════════════════════════════════════════
    private string? BuildOrderBy(DynamicReportInfoResponse info, List<DynamicReportSortItem> userSorts)
    {
        if (info.Report.AllowUserSort && userSorts.Count > 0)
        {
            var sort = userSorts[0];
            var allowed = info.SortableColumns.FirstOrDefault(s =>
                s.FieldName.Equals(sort.Field, StringComparison.OrdinalIgnoreCase));

            if (allowed != null)
            {
                var dir = string.Equals(sort.Dir, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                return $"{allowed.FieldName} {dir}";
            }
        }

        // پیش‌فرض
        var defaultSort = info.SortableColumns.FirstOrDefault(s => s.IsDefault);
        if (defaultSort != null)
        {
            var dir = string.Equals(defaultSort.DefaultDirection, "DESC", StringComparison.OrdinalIgnoreCase)
                ? "DESC" : "ASC";
            return $"{defaultSort.FieldName} {dir}";
        }

        if (!string.IsNullOrEmpty(info.Report.DefaultOrderBy))
            return info.Report.DefaultOrderBy;

        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  Helper: محاسبه Aggregates
    // ═══════════════════════════════════════════════════════════
    private async Task<Dictionary<string, object?>> CalculateAggregatesAsync(
        SqlConnection conn,
        DynamicReportInfoResponse info,
        string baseQuery,
        DynamicParameters parameters)
    {
        var aggregates = new Dictionary<string, object?>();

        var aggColumns = info.Columns
            .Where(c => !string.IsNullOrEmpty(c.AggregateFunc))
            .ToList();

        if (aggColumns.Count == 0) return aggregates;

        var selectParts = aggColumns.Select(c =>
            $"{c.AggregateFunc}(subq.[{c.FieldName}]) AS [{c.FieldName}]");

        var aggSql = $@"
            SELECT {string.Join(",", selectParts)}
            FROM (
                {baseQuery}
            ) AS subq";

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync(aggSql, parameters);
            if (row != null)
            {
                foreach (var kvp in (IDictionary<string, object?>)row)
                {
                    aggregates[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate aggregates for report {ReportId}", info.Report.Id);
        }

        return aggregates;
    }

    // ═══════════════════════════════════════════════════════════
    //  Helper: تبدیل مقدار به نوع SQL
    // ═══════════════════════════════════════════════════════════
    private object? ConvertToSqlValue(object? rawValue, string dataType)
    {
        if (rawValue == null) return null;

        var str = rawValue.ToString();
        if (string.IsNullOrWhiteSpace(str)) return null;

        return dataType.ToLowerInvariant() switch
        {
            "int" => int.TryParse(str, out var i) ? i : throw new ArgumentException($"مقدار «{str}» عدد صحیح نیست."),
            "long" => long.TryParse(str, out var l) ? l : throw new ArgumentException($"مقدار «{str}» عدد صحیح بلند نیست."),
            "decimal" => decimal.TryParse(str, out var d) ? d : throw new ArgumentException($"مقدار «{str}» عدد اعشاری نیست."),
            "bool" => bool.TryParse(str, out var b) ? b : throw new ArgumentException($"مقدار «{str}» بولین نیست."),
            "date" => DateTime.TryParse(str, out var dt) ? dt : throw new ArgumentException($"مقدار «{str}» تاریخ نیست."),
            _ => str
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Helper: دریافت مقدار سیستمی
    // ═══════════════════════════════════════════════════════════
    private object? GetSystemValue(string? systemSource, int currentUserId)
    {
        if (string.IsNullOrEmpty(systemSource))
            throw new ArgumentException("منبع سیستم برای پارامتر سیستمی مشخص نشده است.");

        return systemSource.ToLowerInvariant() switch
        {
            "userid" => currentUserId,
            "companyid" => 1, // TODO: از تنظیمات یا Claims بخوانید
            "branchid" => 1,  // TODO: از تنظیمات یا Claims بخوانید
            _ => throw new ArgumentException($"منبع سیستمی «{systemSource}» پشتیبانی نمی‌شود.")
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Helper: اعتبارسنجی SQL (برای Lookups)
    // ═══════════════════════════════════════════════════════════
    private void ValidateSqlQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException("SQL خالی است.");

        var trimmed = sql.TrimStart();

        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("فقط کوئری‌های SELECT مجاز هستند.");

        var upper = sql.ToUpperInvariant();
        foreach (var kw in DangerousKeywords)
        {
            if (upper.Contains(kw))
                throw new InvalidOperationException($"کلمه‌ی ممنوعه «{kw}» در کوئری یافت شد.");
        }
    }

    private string GetConnectionString()
    {
        var cs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(cs))
            throw new InvalidOperationException("Connection string not configured.");
        return cs;
    }
}