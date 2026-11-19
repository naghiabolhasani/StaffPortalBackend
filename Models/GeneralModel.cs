namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════════
//  Request (بدون تغییر)
// ═══════════════════════════════════════════════════════════════
public class GeneralTemplateMessageRequest
{
    public int Kind { get; set; }
    public string? Message { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Response — تغییر نام + فیلد Id
// ═══════════════════════════════════════════════════════════════
public class GeneralTemplateMessageResponse
{
    public long Id { get; set; }              // ← 🆕 جدید
    public string? Message { get; set; }
}