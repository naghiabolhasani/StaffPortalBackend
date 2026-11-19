using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class SimilarCustomerService : ISimilarCustomerService
{
    // ═══════════════════════════════════════════════════════════════
    //  تنظیمات Cache — قابل تغییر از یک‌جا
    // ═══════════════════════════════════════════════════════════════
    private const int CACHE_IDLE_MINUTES = 5;         // آزادسازى پس از ۵ دقیقه بى‌استفاده
    private const int CACHE_MAX_MINUTES = 15;         // حداکثر عمر Cache حتى با استفاده
    private const string CACHE_KEY = "similar_customer_cache_v1";

    private static readonly TimeSpan CacheIdleTimeout = TimeSpan.FromMinutes(CACHE_IDLE_MINUTES);
    private static readonly TimeSpan CacheMaxLifetime = TimeSpan.FromMinutes(CACHE_MAX_MINUTES);

    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    private readonly string _connectionString;
    private readonly ILogger<SimilarCustomerService> _logger;
    private readonly IMemoryCache _cache;

    public SimilarCustomerService(
        IConfiguration configuration,
        ILogger<SimilarCustomerService> logger,
        IMemoryCache cache)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
        _cache = cache;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tarh List
    // ═══════════════════════════════════════════════════════════════
    public async Task<TarhListResponse> GetTarhListAsync()
    {
        const string sql = @"
SELECT DISTINCT Tarh
FROM DarikAssistant.dbo.ContractGrade WITH (NOLOCK)
WHERE Tarh IS NOT NULL 
  AND LTRIM(RTRIM(Tarh)) <> ''
ORDER BY Tarh;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<string>(sql);

        var list = new List<string> { "همه طرح‌ها" };
        list.AddRange(result.Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => x.Trim()));

        return new TarhListResponse
        {
            Tarhs = list,
            Total = list.Count - 1
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Search — با Cache و Sliding Expiration
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<SimilarCustomerSearchItem>> SearchAsync(
        string? name,
        string? tarh)
    {
        // ─── نرمال‌سازى ورودى ───
        var normName = TextNormalizer.NormalizeForSearch(name);
        var normTarh = TextNormalizer.NormalizeForSearch(tarh);

        if (IsAllTarhsValue(tarh) || IsAllTarhsValue(normTarh))
            normTarh = null;

        // ─── گرفتن داده از Cache (یا ساخت) ───
        var allCustomers = await GetOrBuildCacheAsync();

        // ─── فیلتر در حافظه ───
        var query = allCustomers.AsEnumerable();

        if (normTarh != null)
            query = query.Where(c => c.NormalizedTarh == normTarh);

        if (normName != null)
            query = query.Where(c => c.NormalizedName.Contains(normName));

        return query
            .Take(100)
            .Select(c => new SimilarCustomerSearchItem
            {
                CustomerName = c.CustomerName,
                NationalCode = c.NationalCode,
                ContractNo = c.ContractNo,
                Tarh = c.Tarh,
                FirstInstallmentDate = c.FirstInstallmentDate,
                LastInstallmentDate = c.LastInstallmentDate,
                FirstInstallmentAmount = c.FirstInstallmentAmount
            })
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ساخت / خواندن Cache
    // ═══════════════════════════════════════════════════════════════
    private async Task<List<CachedCustomer>> GetOrBuildCacheAsync()
    {
        // ─── ۱. تلاش براى خواندن از Cache ───
        if (_cache.TryGetValue(CACHE_KEY, out List<CachedCustomer>? cached) && cached != null)
        {
            _logger.LogDebug("Similar-customer cache hit. Sliding window reset to {Minutes} min.",
                CACHE_IDLE_MINUTES);
            return cached;
        }

        // ─── ۲. قفل براى جلوگیرى از ساخت همزمان ───
        await _cacheLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(CACHE_KEY, out cached) && cached != null)
                return cached;

            _logger.LogInformation("Building similar-customer cache...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ─── کوئرى ساده (بدون UDF، بدون REPLACE) ───
            const string sql = @"
SELECT 
    CU.FName + '  ' + CU.LName                     AS CustomerName,
    CU.FName + CU.LName                            AS RawName,
    CU.NationalCode                                AS NationalCode,
    C.ContractNo                                   AS ContractNo,
    G.Tarh                                         AS Tarh,
    FORMAT(I.FirstInstallmentDate, '###/##/##')    AS FirstInstallmentDate,
    FORMAT(I.LastInstallmentDate, '###/##/##')     AS LastInstallmentDate,
    FORMAT(I.FirstInstallmentAmount, 'N0')         AS FirstInstallmentAmount
FROM Leasing.dbo.FUNCustomer CU WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNContract C WITH (NOLOCK)
    ON C.CifRef = CU.Serial
INNER JOIN DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
    ON G.ContractRef = C.Serial
LEFT JOIN Leasing.dbo.[Installment#] I WITH (NOLOCK)
    ON I.ContractRef = C.Serial;";

            await using var connection = new SqlConnection(_connectionString);
            var raw = (await connection.QueryAsync<CachedCustomer>(sql)).ToList();

            // ─── نرمال‌سازى در C# (یک‌بار) ───
            foreach (var c in raw)
            {
                c.NormalizedName = TextNormalizer.NormalizeForSearch(c.RawName) ?? "";
                c.NormalizedTarh = TextNormalizer.NormalizeForSearch(c.Tarh) ?? "";
            }

            // ═══════════════════════════════════════════════════════════
            //  ذخیره در Cache با Sliding Expiration
            //  - اگر ۵ دقیقه هیچ جستجویى نیاید → آزاد مى‌شود
            //  - اگر استفاده شود → ۵ دقیقه ریست مى‌شود
            //  - در هر حالت، حداکثر ۱۵ دقیقه بعد از ساخت، بازسازى مى‌شود
            // ═══════════════════════════════════════════════════════════
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheIdleTimeout,
                AbsoluteExpirationRelativeToNow = CacheMaxLifetime
            };

            // callback آزادسازى — براى لاگ
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                var count = (value as List<CachedCustomer>)?.Count ?? 0;
                _logger.LogInformation(
                    "Similar-customer cache evicted: Reason={Reason}, Records={Count}. Memory freed.",
                    reason, count);
            });

            _cache.Set(CACHE_KEY, raw, options);

            sw.Stop();
            _logger.LogInformation(
                "Cache built: {Count} customers in {Elapsed}ms. " +
                "Idle timeout: {IdleMin} min, Max lifetime: {MaxMin} min.",
                raw.Count, sw.ElapsedMilliseconds, CACHE_IDLE_MINUTES, CACHE_MAX_MINUTES);

            return raw;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  مدل داخلى Cache
    // ═══════════════════════════════════════════════════════════════
    private class CachedCustomer
    {
        public string? CustomerName { get; set; }
        public string? RawName { get; set; }
        public string? NationalCode { get; set; }
        public string? ContractNo { get; set; }
        public string? Tarh { get; set; }
        public string? FirstInstallmentDate { get; set; }
        public string? LastInstallmentDate { get; set; }
        public string? FirstInstallmentAmount { get; set; }

        public string NormalizedName { get; set; } = "";
        public string NormalizedTarh { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper: تشخیص «همه طرح‌ها»
    // ═══════════════════════════════════════════════════════════════
    private static bool IsAllTarhsValue(string? tarh)
    {
        if (string.IsNullOrWhiteSpace(tarh)) return true;

        var cleaned = tarh.Trim()
            .Replace(" ", "")
            .Replace("\u200C", "")
            .Replace("\u200D", "");

        return cleaned == "همهطرحها" ||
               cleaned == "همهطرحها" ||
               cleaned == "همهطرحها";
    }
}