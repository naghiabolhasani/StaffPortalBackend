namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════════
//  Request — استعلام راهبر
// ═══════════════════════════════════════════════════════════════
public class RahbarInquiryRequest
{
    public string? NationalCode { get; set; }         // کد ملى مشترى
    public string? LoanReferenceCode { get; set; }    // شناسه تسهیلات
}

// ═══════════════════════════════════════════════════════════════
//  Result
// ═══════════════════════════════════════════════════════════════
public class RahbarInquiryResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int TotalInstallments { get; set; }
    public long TotalPayableAmount { get; set; }
    public int DueInstallments { get; set; }
    public List<RahbarInstallmentItem> Installments { get; set; } = new();
}

public class RahbarInstallmentItem
{
    public int Number { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public long InstallmentAmount { get; set; }
    public long PenaltyAmount { get; set; }
    public long TotalPayableAmount { get; set; }
    public bool IsDue { get; set; }
}



// ═══════════════════════════════════════════════════════════════
//  Bulk Inquiry Result
// ═══════════════════════════════════════════════════════════════
public class RahbarBulkInquiryResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    public int TotalProcessed { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

}


// ═══════════════════════════════════════════════════════════════
//  Internal pair
// ═══════════════════════════════════════════════════════════════
internal class RahbarPair
{
    public string? NationalCode { get; set; }
    public string? InstallmentNo { get; set; }
}