namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════════
//  CustomerModel — مدل اصلى مشترى
//  ⚠️ این کلاس قابل توسعه است: هر فیلد جدیدى که لازم شد اضافه کن
// ═══════════════════════════════════════════════════════════════
public class CustomerModel
{
    public string? CustomerName { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }      // ← جدید
    public string? Tarh { get; set; }            // ← جدید

    // 📌 فیلدهاى آینده (در صورت نیاز باز کن):
    // public long? Serial { get; set; }
    // public string? Mobile { get; set; }
    // public string? ContractNo { get; set; }
    // public string? Tarh { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Request — جستجو بر اساس نام
// ═══════════════════════════════════════════════════════════════
public class CustomerNameRequest
{
    public string? Name { get; set; }
}