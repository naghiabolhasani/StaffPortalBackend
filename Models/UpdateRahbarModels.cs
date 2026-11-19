namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════════
//  Request
// ═══════════════════════════════════════════════════════════════
public class UpdateRahbarRequest
{
    public string? ContractNo { get; set; }
    public string? InstallmentNo { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Result — فقط دو فیلد
// ═══════════════════════════════════════════════════════════════
public class UpdateRahbarResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    public long TotalPayableAmount { get; set; }
    public int ArrearsInstallmentCount { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  API DTOs (پاسخ Golrang)
// ═══════════════════════════════════════════════════════════════
public class InquiryApiResponse
{
    public bool Success { get; set; }
    public List<InquiryApiMessage>? Messages { get; set; }
    public List<InquiryApiInstallment>? Result { get; set; }
}

public class InquiryApiMessage
{
    public string? Message { get; set; }
}

public class InquiryApiInstallment
{
    public int Number { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public long InstallmentAmount { get; set; }
    public long PenaltyAmount { get; set; }
    public long TotalPayableAmount { get; set; }
}