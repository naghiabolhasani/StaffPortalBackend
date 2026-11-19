using System;
using System.ComponentModel.DataAnnotations;

namespace StaffPortal.Models;

// ============================================================
// ورودی جستجو
// ============================================================
public class ContractFacilitySearchRequest
{
    [Required(ErrorMessage = "شماره قرارداد اجباری است.")]
    public string ContractNo { get; set; } = string.Empty;
}

// ============================================================
// خروجی جستجو
// ============================================================
public class ContractFacilityDto
{
    public string? NationalCode { get; set; }
    public string? CustomerName { get; set; }
    public long? FirstInstallmentAmount { get; set; }
    public string? FirstInstallmentDate { get; set; }
    public DateTime? FirstInstallmentDateEn { get; set; }
    public long? LastInstallmentAmount { get; set; }
    public string? LastInstallmentDate { get; set; }
    public DateTime? LastInstallmentDateEn { get; set; }
    public long? InsNaghdi { get; set; }
    public long? InsGhesti { get; set; }
    public string? AgnName { get; set; }
    public long? SumDebt { get; set; }
    public string? RatificationDate { get; set; }
    public DateTime? RatificationDateEn { get; set; }
    public string? InstallmentNo { get; set; }
}

// ============================================================
// ورودی به‌روزرسانی شماره تسهیلات
// ============================================================
public class UpdateFacilityIdRequest
{
    [Required(ErrorMessage = "شماره قرارداد اجباری است.")]
    public string ContractNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "شناسه تسهیلات اجباری است.")]
    public string FacilityId { get; set; } = string.Empty;
}

// ============================================================
// ورودی به‌روزرسانی تاریخ تصویب
// ============================================================
public class UpdateRatificationDateRequest
{
    [Required(ErrorMessage = "شماره قرارداد اجباری است.")]
    public string ContractNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "تاریخ تصویب اجباری است.")]
    public string RatificationDate { get; set; } = string.Empty;
}