using StaffPortal.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StaffPortal.Interfaces;

public interface IRahbarService
{
    /// <summary>
    /// فراخوانى خام API استعلام (براى استفاده‌ى داخلى سرویس‌ها)
    /// </summary>
    Task<(bool Success, string? Message, List<InquiryApiInstallment> Installments)>
        CallInquiryApiAsync(string nationalCode, string loanReferenceCode);

    /// <summary>
    /// استعلام کامل با محاسبات (براى اندپوینت)
    /// </summary>
    Task<RahbarInquiryResult> InquiryAsync(RahbarInquiryRequest request);

    Task<RahbarBulkInquiryResult> RunBulkInquiryAsync(CancellationToken ct = default);

}