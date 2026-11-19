using System.Collections.Generic;
using System.Threading.Tasks;
using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IDynamicReportService
{
    Task<List<ReportCategoryDto>> GetReportsForUserAsync(int userId);
    Task<DynamicReportInfoResponse> GetReportInfoAsync(int reportId);
    Task<DynamicReportRunResponse> RunReportAsync(DynamicReportRunRequest request, int currentUserId);
}