using System.Collections.Generic;

namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════
// 📌 Request Models
// ═══════════════════════════════════════════════════════════

public class DynamicReportListRequest
{
    public int UserId { get; set; }
}

public class DynamicReportInfoRequest
{
    public int ReportId { get; set; }
}

public class DynamicReportRunRequest
{
    public int ReportId { get; set; }
    public Dictionary<string, object?> RequiredParams { get; set; } = new();
    public List<DynamicReportFilterItem> Filters { get; set; } = new();
    public List<DynamicReportSortItem> Sorts { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class DynamicReportFilterItem
{
    public string Field { get; set; } = string.Empty;
    public string Op { get; set; } = string.Empty;
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public List<string>? Values { get; set; }
}

public class DynamicReportSortItem
{
    public string Field { get; set; } = string.Empty;
    public string Dir { get; set; } = "ASC";
}

// ═══════════════════════════════════════════════════════════
// 📌 Response Models
// ═══════════════════════════════════════════════════════════

public class ReportCategoryDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentRef { get; set; }
    public int DisplayOrder { get; set; }
    public List<DynamicReportListItemDto> Reports { get; set; } = new();
}

public class DynamicReportListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Dsc { get; set; }
    public int? CategoryId { get; set; }
    public string? PermissionRef { get; set; }
}

public class DynamicReportInfoResponse
{
    public ReportMetaDto Report { get; set; } = new();
    public List<ReportParameterDto> Parameters { get; set; } = new();
    public List<ReportOptionalFilterDto> Filters { get; set; } = new();
    public List<ReportSortableColumnDto> SortableColumns { get; set; } = new();
    public List<ReportColumnDto> Columns { get; set; } = new();
    public Dictionary<int, List<LookupItemDto>> Lookups { get; set; } = new();
}

public class ReportMetaDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Dsc { get; set; }
    public int? CategoryId { get; set; }
    public string BaseQuery { get; set; } = string.Empty;
    public string? DefaultOrderBy { get; set; }
    public bool AllowUserSort { get; set; }
    public bool AllowUserFilter { get; set; }
    public bool AllowPaging { get; set; }
    public int DefaultPageSize { get; set; }
    public int MaxPageSize { get; set; }
    public int Version { get; set; }
}

public class ReportParameterDto
{
    public int Id { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public string ParamNameWithoutAt { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public byte ParamKind { get; set; }
    public string? SystemSource { get; set; }
    public string WhereExpression { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public string? DefaultValue { get; set; }
    public string? ControlType { get; set; }
    public int? LookupId { get; set; }
    public string? ValidationRegex { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public int DisplayOrder { get; set; }
}

public class ReportOptionalFilterDto
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string AllowedOps { get; set; } = string.Empty;
    public string? DefaultOp { get; set; }
    public string? ControlType { get; set; }
    public int? LookupRef { get; set; }
    public bool IsMultiSelect { get; set; }
    public int DisplayOrder { get; set; }
}

public class ReportSortableColumnDto
{
    public int SortId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? DefaultDirection { get; set; }
    public int DisplayOrder { get; set; }
}

public class ReportColumnDto
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public string? Format { get; set; }
    public string? Align { get; set; }
    public int? Width { get; set; }
    public bool IsVisible { get; set; }
    public string? AggregateFunc { get; set; }
    public int DisplayOrder { get; set; }
}

public class LookupItemDto
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class DynamicReportRunResponse
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<ReportColumnDto> Columns { get; set; } = new();
    public Dictionary<string, object?> Aggregates { get; set; } = new();
}