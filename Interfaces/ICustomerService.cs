using StaffPortal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StaffPortal.Interfaces;

/// <summary>
/// سرویس مشترى — قابل توسعه با متدهاى جدید (GetById, GetByNationalCode, ...)
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// جستجو بر اساس نام — حداکثر ۱۰۰ نتیجه
    /// </summary>
    Task<List<CustomerModel>> SearchByNameAsync(
        string? name,
        string? mobile,
        string? nationalCode,
        string? contractNo,
        string? installmentNo);

    // 📌 متدهاى آینده (در صورت نیاز باز کن):
    // Task<CustomerModel?> GetByNationalCodeAsync(string nationalCode);
    // Task<CustomerModel?> GetByContractNoAsync(string contractNo);
    // Task<List<CustomerModel>> SearchByTarhAsync(string tarh);
}