namespace StaffPortal.Models;

public class ReceiveSmsModels
{
    public string FromDate { get; set; } = string.Empty;
    public string ToDate { get; set; } = string.Empty;
    public string? Mobile { get; set; }

    public int PageNumber { get; set; } = 1;    // شماره صفحه (از ۱ شروع می‌شود)
    public int PageSize { get; set; } = 50;     // تعداد رکورد در هر صفحه
}


public class SmsReportItem
{
    public long Id { get; set; }
    public string? ApiId { get; set; }
    public string? Mobile { get; set; }
    public string? Message { get; set; }
    public string? SendDate { get; set; }
    public string? SendTime { get; set; }
    public string? CustomerName { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
    public string? LastPayDate { get; set; }
    public long? Moavagh { get; set; }
    public string? Tarh { get; set; }
    public bool? IsAnswered { get; set; }
    public string? AnsweredUserName { get; set; }
    public string? AnsweredMessage { get; set; }
}

public class AdpReceiveDebugRequest
{
    /// <summary>
    /// تاریخ شمسی (فقط برای فیلتر نمایش، مثال: 1405/07/08)
    /// </summary>
    public string ShamsiDate { get; set; } = string.Empty;
}


public class AdpReceiveDebugResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RawSoapResponse { get; set; }      // پاسخ خام SOAP
    public int TotalMessagesReturned { get; set; }    // تعداد کل پیامک‌های برگشتی
    public int MessagesMatchingDate { get; set; }     // تعداد پیامک‌های مطابق تاریخ
    public List<AdpMessageDto> Messages { get; set; } = new();  // لیست پیامک‌ها
    public string? StatusCode { get; set; }           // کد وضعیت سرویس
    public string? RequestSoap { get; set; }          // درخواست SOAP ارسالی
}

public class AdpMessageDto
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Time { get; set; }                 // زمان اصلی (میلادی)
    public string? ShamsiDate { get; set; }           // تاریخ شمسی
    public string? ShamsiTime { get; set; }           // ساعت شمسی
    public string? Type { get; set; }
    public string? Encoding { get; set; }
    public string? Content { get; set; }
    public string? Status { get; set; }
}

public class CustomerSmsInfo
{
    public string? CleanMobile { get; set; }
    public string? CustomerName { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
    public int? LastPayDate { get; set; }
    public long? Moavagh { get; set; }
    public string? Tarh { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Update Customer Info on SmsReport
// ═══════════════════════════════════════════════════════════════
public class UpdateCustomerInfoRequest
{
    public int Id { get; set; }
    public string? ContractNo { get; set; }
}

public class UpdateCustomerInfoResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int AffectedRows { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Set Alarm — ثبت پاسخ + هشدار
// ═══════════════════════════════════════════════════════════════
public class SetAlarmRequest
{
    public int Id { get; set; }
    public string? AnsweredMessage { get; set; }
    public string? ContractNo { get; set; }
}

public class SetAlarmResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public long? ContractSerial { get; set; }
    public int AffectedRows { get; set; }
}

public class ReceiveSmsRequest
{
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public string? Mobile { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ReceiveSmsResult
{
    public List<SmsReportItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class DebugSmsItem
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Time { get; set; }
    public string? TimeShamsi { get; set; }
    public string? Content { get; set; }
    public string? Type { get; set; }
    public string? Encoding { get; set; }
    public string? Status { get; set; }
}

public class DebugSmsResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int TotalFetched { get; set; }
    public string? MinShamsiDate { get; set; }
    public string? MaxShamsiDate { get; set; }
    public List<DebugSmsItem> Items { get; set; } = new();
}
