using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface ISimilarCustomerService
{
    Task<TarhListResponse> GetTarhListAsync();

    Task<List<SimilarCustomerSearchItem>> SearchAsync(
        string? name,
        string? tarh);
}