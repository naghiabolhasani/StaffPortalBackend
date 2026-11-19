using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StaffPortal.Models;

public class StandardResponse<T>
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    public int Count { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationInfo? Pagination { get; set; }

    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    // ============================================================
    //  متدهای کمکی
    // ============================================================

    /// <summary>
    /// پاسخ موفق برای یک آبجکت تکی
    /// </summary>
    public static StandardResponse<T> Success(T data, string? message = null)
    {
        return new StandardResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Count = 1,
            Message = message ?? string.Empty
        };
    }

    /// <summary>
    /// پاسخ موفق برای لیست (بدون پیجینگ)
    /// </summary>
    public static StandardResponse<List<TItem>> SuccessList<TItem>(
        List<TItem> data,
        string? message = null)
    {
        return new StandardResponse<List<TItem>>
        {
            IsSuccess = true,
            Data = data,
            Count = data?.Count ?? 0,
            Message = message ?? string.Empty
        };
    }

    /// <summary>
    /// پاسخ موفق برای لیست با پیجینگ
    /// </summary>
    public static StandardResponse<List<TItem>> SuccessPaged<TItem>(
        List<TItem> data,
        PaginationInfo pagination,
        string? message = null)
    {
        return new StandardResponse<List<TItem>>
        {
            IsSuccess = true,
            Data = data,
            Count = data?.Count ?? 0,
            Pagination = pagination,
            Message = message ?? string.Empty
        };
    }

    /// <summary>
    /// پاسخ ناموفق
    /// </summary>
    public static StandardResponse<object> Failure(string message)
    {
        return new StandardResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Count = 0,
            Message = message
        };
    }
}

// ============================================================
//  کلاس متادیتای پیجینگ
// ============================================================
public class PaginationInfo
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}