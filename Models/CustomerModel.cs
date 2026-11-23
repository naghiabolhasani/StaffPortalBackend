namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════════
//  CustomerModel — مدل اصلى مشترى
//  ⚠️ این کلاس قابل توسعه است: هر فیلد جدیدى که لازم شد اضافه کن
// ═══════════════════════════════════════════════════════════════
public class CustomerModel
{
    public string? CustomerName { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
    public string? InstallmentNo { get; set; }
    public string? Tarh { get; set; }
    public string? Mobile1 { get; set; }
    public string? Mobile2 { get; set; }

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
    public string? Mobile { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
    public string? InstallmentNo { get; set; }
}