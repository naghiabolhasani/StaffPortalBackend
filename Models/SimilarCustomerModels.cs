namespace StaffPortal.Models;

// ═══════════════════════════════════════════════════════════════
//  Tarh List
// ═══════════════════════════════════════════════════════════════
public class TarhListResponse
{
    public List<string> Tarhs { get; set; } = new();
    public int Total { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Search
// ═══════════════════════════════════════════════════════════════
public class SimilarCustomerSearchRequest
{
    public string? Name { get; set; }
    public string? Tarh { get; set; }
}

public class SimilarCustomerSearchItem
{
    public string? CustomerName { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
    public string? Tarh { get; set; }
    public string? FirstInstallmentDate { get; set; }
    public string? LastInstallmentDate { get; set; }
    public string? FirstInstallmentAmount { get; set; }
}