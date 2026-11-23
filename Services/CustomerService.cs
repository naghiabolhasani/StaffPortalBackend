using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class CustomerService : ICustomerService
{
    private const int MAX_RESULTS = 100;

    private readonly string _connectionString;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IConfiguration configuration, ILogger<CustomerService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<List<CustomerModel>> SearchByNameAsync(
        string? name,
        string? mobile,
        string? nationalCode,
        string? contractNo,
        string? installmentNo)
    {
        var nameNorm = TextNormalizer.NormalizeForSearch(name);
        var mobileCore = CoreMobile(mobile);
        var national = DigitsOnly(nationalCode);
        var contract = string.IsNullOrWhiteSpace(contractNo) ? "" : contractNo.Trim();
        var installment = string.IsNullOrWhiteSpace(installmentNo) ? "" : installmentNo.Trim();

        var filters = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("MaxResults", MAX_RESULTS);
        parameters.Add("ConcatType", AppConstants.ContactRelationMain);

        if (!string.IsNullOrEmpty(nameNorm))
        {
            filters.Add(@"
REPLACE(REPLACE(REPLACE(ISNULL(CU.FName, N'') + ISNULL(CU.LName, N''), N' ', N''), N'ی', N'ي'), N'ک', N'ك') LIKE @NameLike");
            parameters.Add("NameLike", "%" + nameNorm + "%");
        }

        if (!string.IsNullOrEmpty(mobileCore))
        {
            filters.Add(@"
(
    cc.Mobile1 LIKE @MobileLike
    OR cc.Mobile2 LIKE @MobileLike
)");
            parameters.Add("MobileLike", "%" + mobileCore + "%");
        }

        if (!string.IsNullOrEmpty(national))
        {
            filters.Add("CU.NationalCode LIKE @NationalLike");
            parameters.Add("NationalLike", "%" + national + "%");
        }

        if (!string.IsNullOrEmpty(contract))
        {
            filters.Add("C.ContractNo LIKE @ContractLike");
            parameters.Add("ContractLike", "%" + contract + "%");
        }

        if (!string.IsNullOrEmpty(installment))
        {
            filters.Add("C.InstallmentNo LIKE @InstallmentLike");
            parameters.Add("InstallmentLike", "%" + installment + "%");
        }

        if (filters.Count == 0)
            return new List<CustomerModel>();

        var sql = $@"
SELECT DISTINCT TOP (@MaxResults)
    CU.FName + '  ' + CU.LName AS CustomerName,
    CU.NationalCode            AS NationalCode,
    C.ContractNo               AS ContractNo,
    C.InstallmentNo            AS InstallmentNo,
    G.Tarh                     AS Tarh,
    cc.Mobile1                 AS Mobile1,
    cc.Mobile2                 AS Mobile2
FROM Leasing.dbo.FUNCustomer CU WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNContract C WITH (NOLOCK)
    ON C.CifRef = CU.Serial
INNER JOIN DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
    ON G.ContractRef = C.Serial
LEFT JOIN Leasing.dbo.FunCustomerContacts cc WITH (NOLOCK)
    ON cc.CifRef = CU.Serial
   AND cc.ConcatType = @ConcatType
WHERE {string.Join(" AND ", filters)};";

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<CustomerModel>(sql, parameters);
        var list = rows.ToList();

        _logger.LogInformation("Customer search returned {Count} rows.", list.Count);
        return list;
    }

    private static string DigitsOnly(string? value)
    {
        var normalized = TextNormalizer.NormalizeForSearch(value) ?? "";
        return new string(normalized.Where(char.IsDigit).ToArray());
    }

    private static string CoreMobile(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.StartsWith("98") && digits.Length > 10)
            digits = digits[2..];
        if (digits.StartsWith("0"))
            digits = digits[1..];
        return digits;
    }
}
