using System;
using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Services;

public class ContractFacilityService : IContractFacilityService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContractFacilityService> _logger;

    public ContractFacilityService(IConfiguration configuration, ILogger<ContractFacilityService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        var cs = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(cs))
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        return cs;
    }

    // ============================================================
    // ۱. جستجوی تسهیلات قرارداد
    // ============================================================
    public async Task<ContractFacilityDto?> SearchAsync(string contractNo)
    {
        using var connection = new SqlConnection(GetConnectionString());

        const string sql = @"
        SELECT DISTINCT
            CU.NationalCode,
            CU.fname + ' ' + CU.lname AS CustomerName,
            ins.FirstInstallmentAmount,
            ins.FirstInstallmentDate,
            ins.LastInstallmentAmount,
            ins.LastInstallmentDate,
            L.InsNaghdi,
            L.InsGhesti,
            A.AgnName,
            ins.SumDebt,
            co.RatificationDate,
            co.InstallmentNo
        FROM leasing..FUNContract co 
        LEFT JOIN leasing..[Installment#] ins ON ins.ContractRef = co.Serial
        LEFT JOIN leasing..FUNCustomer CU ON co.CifRef = CU.Serial
        LEFT JOIN leasing..FunCustomerContacts cc ON cc.CifRef = cu.Serial AND cc.DefaultAddress = 1 And cc.ConcatType=2431
        LEFT JOIN leasing..[Clear#] L ON co.Serial = L.contractref
        LEFT JOIN leasing..BASAgency A ON co.DeliverAgnRef = A.Serial
        WHERE co.ContractNo = @ContractNo
    ";

        var raw = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { ContractNo = contractNo });
        if (raw == null)
            return null;

        var dto = new ContractFacilityDto
        {
            NationalCode = raw.NationalCode,
            CustomerName = raw.CustomerName,
            FirstInstallmentAmount = raw.FirstInstallmentAmount,
            LastInstallmentAmount = raw.LastInstallmentAmount,
            InsNaghdi = raw.InsNaghdi,
            InsGhesti = raw.InsGhesti,
            AgnName = raw.AgnName,
            SumDebt = raw.SumDebt,
            InstallmentNo = raw.InstallmentNo?.ToString(),
        };

        // ✅ تعریف صریح متغیرهای out
        DateTime? firstEn;
        DateTime? lastEn;
        DateTime? ratEn;

        dto.FirstInstallmentDate = ConvertPersianDateToString(raw.FirstInstallmentDate, out firstEn);
        dto.FirstInstallmentDateEn = firstEn;

        dto.LastInstallmentDate = ConvertPersianDateToString(raw.LastInstallmentDate, out lastEn);
        dto.LastInstallmentDateEn = lastEn;

        dto.RatificationDate = ConvertPersianDateToString(raw.RatificationDate, out ratEn);
        dto.RatificationDateEn = ratEn;

        return dto;
    }
    // ============================================================
    // ۲. به‌روزرسانی شماره تسهیلات
    // ============================================================
    public async Task<bool> UpdateFacilityIdAsync(string contractNo, string facilityId)
    {
        using var connection = new SqlConnection(GetConnectionString());

        const string sql = @"
            UPDATE co 
            SET co.InstallmentNo = @FacilityId 
            FROM leasing..FUNContract co 
            WHERE co.ContractNo = @ContractNo
        ";

        var rows = await connection.ExecuteAsync(sql, new
        {
            ContractNo = contractNo,
            FacilityId = facilityId
        });

        if (rows == 0)
            throw new Exception("قراردادی با این شماره یافت نشد.");

        _logger.LogInformation(
            "InstallmentNo قرارداد {ContractNo} به {FacilityId} تغییر یافت.",
            contractNo, facilityId);

        return true;
    }

    // ============================================================
    // ۳. به‌روزرسانی تاریخ تصویب
    // ============================================================
    public async Task<bool> UpdateRatificationDateAsync(string contractNo, string ratificationDate)
    {
        if (!IsValidPersianDate(ratificationDate, out _))
            throw new Exception("تاریخ وارد شده معتبر نیست. فرمت صحیح: yyyymmdd (مثلاً 14040112)");

        using var connection = new SqlConnection(GetConnectionString());

        var dateInt = int.Parse(ratificationDate);

        const string sql = @"
            UPDATE co 
            SET co.RatificationDate = @Date 
            FROM leasing..FUNContract co 
            WHERE co.ContractNo = @ContractNo
        ";

        var rows = await connection.ExecuteAsync(sql, new
        {
            ContractNo = contractNo,
            Date = dateInt
        });

        if (rows == 0)
            throw new Exception("قراردادی با این شماره یافت نشد.");

        _logger.LogInformation(
            "RatificationDate قرارداد {ContractNo} به {Date} تغییر یافت.",
            contractNo, ratificationDate);

        return true;
    }

    // ============================================================
    // متدهای کمکی
    // ============================================================
    private bool IsValidPersianDate(string dateStr, out DateTime gregorian)
    {
        gregorian = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(dateStr) || dateStr.Length != 8)
            return false;

        if (!int.TryParse(dateStr, out var dateInt) || dateInt <= 0)
            return false;

        int year = dateInt / 10000;
        int month = (dateInt / 100) % 100;
        int day = dateInt % 100;

        if (year < 1300 || year > 1500 || month < 1 || month > 12 || day < 1 || day > 31)
            return false;

        try
        {
            var pc = new PersianCalendar();
            gregorian = pc.ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string? ConvertPersianDateToString(object? dateValue, out DateTime? gregorian)
    {
        gregorian = null;

        if (dateValue == null || dateValue == DBNull.Value)
            return null;

        try
        {
            if (dateValue is DateTime dt)
            {
                gregorian = dt;
                var pc = new PersianCalendar();
                return $"{pc.GetYear(dt):0000}{pc.GetMonth(dt):00}{pc.GetDayOfMonth(dt):00}";
            }

            var dateInt = Convert.ToInt32(dateValue);
            var dateStr = dateInt.ToString();

            if (dateStr.Length == 8 && IsValidPersianDate(dateStr, out var g))
            {
                gregorian = g;
                return dateStr;
            }

            return dateStr;
        }
        catch
        {
            return null;
        }
    }
}