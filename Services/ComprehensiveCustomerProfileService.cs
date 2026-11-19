using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class ComprehensiveCustomerProfileService : IComprehensiveCustomerProfileService
{
    // ═══════════════════════════════════════════════════════════
    //  فیلدها
    // ═══════════════════════════════════════════════════════════
    private readonly string _connectionString;
    private readonly ILogger<ComprehensiveCustomerProfileService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IRahbarService _rahbarService;
    public ComprehensiveCustomerProfileService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IRahbarService rahbarService,
        ILogger<ComprehensiveCustomerProfileService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _rahbarService = rahbarService;

    }

    // ═══════════════════════════════════════════════════════════════
    //  Base Info (Query 1)
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<ComprehensiveBaseInfoItem>> GetBaseInfoAsync(
        string? nationalCode,
        string? contractNo)
    {
        //        const string sql = @"
        //DECLARE @NC NVARCHAR(20) = @NationalCode;
        //DECLARE @CN NVARCHAR(50) = @ContractNo;

        //DECLARE @ContractIDs TABLE (Serial BIGINT PRIMARY KEY);

        //IF @NC <> '' 
        //BEGIN
        //    INSERT INTO @ContractIDs (Serial)
        //    SELECT C.Serial
        //    FROM [Leasing].dbo.FUNContract C WITH (NOLOCK)
        //    INNER JOIN [Leasing].dbo.FUNCustomer CU WITH (NOLOCK) 
        //        ON C.CifRef = CU.Serial
        //    WHERE CU.NationalCode = @NC;
        //END

        //IF @CN <> '' 
        //BEGIN
        //    INSERT INTO @ContractIDs (Serial)
        //    SELECT C.Serial
        //    FROM [Leasing].dbo.FUNContract C WITH (NOLOCK)
        //    WHERE C.ContractNo = @CN
        //      AND NOT EXISTS (SELECT 1 FROM @ContractIDs WHERE Serial = C.Serial);
        //END

        //IF NOT EXISTS (SELECT 1 FROM @ContractIDs)
        //BEGIN
        //    SELECT TOP 0 
        //        CAST(NULL AS BIGINT)        AS ContractSerial,
        //        CAST(NULL AS NVARCHAR(200)) AS FullName,
        //        CAST(NULL AS NVARCHAR(20))  AS NationalCode,
        //        CAST(NULL AS NVARCHAR(50))  AS ContractNo,
        //        CAST(NULL AS NVARCHAR(200)) AS SourceName,
        //        CAST(NULL AS BIGINT)        AS SourceRef,
        //        CAST(NULL AS NVARCHAR(50))  AS InstallmentNo,
        //        CAST(NULL AS NVARCHAR(200)) AS ContractStatus,
        //        CAST(NULL AS NVARCHAR(200)) AS PlanName,
        //        CAST(NULL AS NVARCHAR(50))  AS AccountNo,
        //        CAST(NULL AS NVARCHAR(50))  AS Mobile1,
        //        CAST(NULL AS NVARCHAR(50))  AS Mobile2;
        //    RETURN;
        //END

        //SELECT 
        //    C.Serial                    AS ContractSerial,
        //    CU.FName + N' ' + CU.LName  AS FullName,
        //    CU.NationalCode             AS NationalCode,
        //    C.ContractNo                AS ContractNo,
        //    D.Title                     AS SourceName,
        //    BP.SourceRef                AS SourceRef,
        //    C.InstallmentNo             AS InstallmentNo,
        //    BL.Title                    AS ContractStatus,
        //    Case When U.FullName Is Not Null Then U.FullName Else G.Tarh End As PlanName,
        //	A.AgnName As AgnName,
        //    N.AccountNo                 AS AccountNo,
        //    cc.Mobile1                  AS Mobile1,
        //    cc.Mobile2                  AS Mobile2
        //FROM DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
        //INNER JOIN @ContractIDs CT ON G.ContractRef = CT.Serial
        //INNER JOIN [Leasing].dbo.FUNContract C WITH (NOLOCK) 
        //    ON G.ContractRef = C.Serial
        //INNER JOIN Leasing.dbo.BASProtocolDetail BPD WITH (NOLOCK) 
        //    ON C.PrtDetailRef = BPD.Serial
        //INNER JOIN Leasing.dbo.BASProtocol BP WITH (NOLOCK) 
        //    ON BPD.ProtocolRef = BP.Serial
        //INNER JOIN Leasing.dbo.ACCDl D WITH (NOLOCK)
        //    ON BP.SourceRef = D.Serial
        //INNER JOIN [Leasing].dbo.FUNCustomer CU WITH (NOLOCK) 
        //    ON C.CifRef = CU.Serial
        //Inner Join Leasing..BASAgency A  WITH (NOLOCK) ON C.DeliverAgnRef = A.Serial
        //Left Outer Join Leasing..UGSRelation R WITH (NOLOCK) ON C.Serial = R.ContractRef
        //Left Outer Join Leasing..UGSInfo U WITH (NOLOCK) ON R.GRef = U.Serial
        //LEFT JOIN [Leasing].dbo.FunCustomerContacts cc WITH (NOLOCK) 
        //    ON cc.CifRef = CU.Serial 
        //   AND cc.ConcatType = 2431

        //LEFT JOIN [Leasing].dbo.BASLookup BL WITH (NOLOCK) 
        //    ON C.Status = BL.Code 
        //   AND BL.Type = 'status'
        //LEFT JOIN (
        //    SELECT 
        //        S.[شماره قرارداد] AS ContractNo, 
        //        S.[شماره حساب]    AS AccountNo
        //    FROM [ImportedFiles].dbo.Sepah_InstallmentNo S WITH (NOLOCK)
        //    WHERE S.IsDefault = 1 
        //    UNION
        //    SELECT 
        //        S.[شماره قرارداد] AS ContractNo, 
        //        S.[شماره سپرده]    AS AccountNo
        //    FROM [ImportedFiles].dbo.Sepah_AhvazYekta_InstallmentNo S WITH (NOLOCK)
        //    WHERE S.[شماره قرارداد] IS NOT NULL
        //    UNION
        //    SELECT 
        //        S.ContractNo AS ContractNo, 
        //        S.EAccNo     AS AccountNo
        //    FROM [ImportedFiles].dbo.Sepah_Ahvaz S WITH (NOLOCK)
        //    WHERE S.ContractNo IS NOT NULL
        //) N 
        //    ON C.ContractNo = N.ContractNo;

        //";

        const string sql = @"
SELECT
    C.Serial                   AS ContractSerial,
    CU.FName + N' ' + CU.LName  AS FullName,
    CU.NationalCode            AS NationalCode,
    C.ContractNo               AS ContractNo,
    D.Title                    AS SourceName,
    BP.SourceRef               AS SourceRef,
    C.InstallmentNo            AS InstallmentNo,
    BL.Title                   AS ContractStatus,
    CASE
        WHEN U.FullName IS NOT NULL THEN U.FullName
        ELSE G.Tarh
    END                        AS PlanName,
    A.AgnName                  AS AgnName,
    N.AccountNo                AS AccountNo,
    cc.Mobile1                 AS Mobile1,
    cc.Mobile2                 AS Mobile2
FROM [Leasing].dbo.FUNContract C WITH (NOLOCK)
INNER JOIN Leasing.dbo.BASProtocolDetail BPD WITH (NOLOCK)
    ON C.PrtDetailRef = BPD.Serial
INNER JOIN Leasing.dbo.BASProtocol BP WITH (NOLOCK)
    ON BPD.ProtocolRef = BP.Serial
INNER JOIN Leasing.dbo.ACCDl D WITH (NOLOCK)
    ON BP.SourceRef = D.Serial
INNER JOIN [Leasing].dbo.FUNCustomer CU WITH (NOLOCK)
    ON C.CifRef = CU.Serial
INNER JOIN Leasing..BASAgency A WITH (NOLOCK)
    ON C.DeliverAgnRef = A.Serial
LEFT JOIN Leasing..UGSRelation R WITH (NOLOCK)
    ON C.Serial = R.ContractRef
LEFT JOIN Leasing..UGSInfo U WITH (NOLOCK)
    ON R.GRef = U.Serial
LEFT JOIN [Leasing].dbo.FunCustomerContacts cc WITH (NOLOCK)
    ON cc.CifRef = CU.Serial
   AND cc.ConcatType = 2431
LEFT JOIN DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
    ON C.Serial = G.ContractRef
LEFT JOIN [Leasing].dbo.BASLookup BL WITH (NOLOCK)
    ON C.Status = BL.Code
   AND BL.Type = 'status'
LEFT JOIN (
    SELECT
        S.[شماره قرارداد] AS ContractNo,
        S.[شماره حساب]    AS AccountNo
    FROM [ImportedFiles].dbo.Sepah_InstallmentNo S WITH (NOLOCK)
    WHERE S.IsDefault = 1

    UNION

    SELECT
        S.[شماره قرارداد] AS ContractNo,
        S.[شماره سپرده]   AS AccountNo
    FROM [ImportedFiles].dbo.Sepah_AhvazYekta_InstallmentNo S WITH (NOLOCK)
    WHERE S.[شماره قرارداد] IS NOT NULL
) N
    ON C.ContractNo = N.ContractNo
WHERE
    (@NationalCode <> N'' AND CU.NationalCode = @NationalCode)
    OR
    (@ContractNo <> N'' AND C.ContractNo = @ContractNo);
";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<ComprehensiveBaseInfoItem>(sql, new
        {
            NationalCode = nationalCode ?? string.Empty,
            ContractNo = contractNo ?? string.Empty
        });

        return result.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Pay Detail (Query 2)
    // ═══════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════
    //  Pay Detail (Query 2)
    // ═══════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════
    //  Pay Detail (Query 2)
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<ComprehensivePayDetailItem>> GetPayDetailAsync(long contractRef)
    {
        const string sql = @"

DECLARE @TodayDate CHAR(8) = LEFT(FORMAT(GETDATE(), 'yyyyMMdd', 'fa-ir'), 8);


SELECT 
    C.ContractNo,
    C.Serial AS ContractSerial,
    G.Moavagh AS Arrear,
    CASE 
        WHEN G.Moavagh < 0 THEN 0
        ELSE CEILING(G.Moavagh * 1.0 / P2.Ghest)
    END AS UnpaidInstallmentCount,
    ISNULL(I.Amount, 0) + ISNULL(H.Amount, 0) - ISNULL(Esterdad.Amount, 0) + ISNULL(S.Amount, 0) AS TotalDeposit,
    G.LastpayDate,
    POld.Ghest AS OldInstallment,--اقساط سررسید شده
    PAti.Ghest AS NextInstallment,
    ReturnCash.Amount AS ReturnCashAmount,
    ReturnCash.Cnt AS ReturnCashCount,
    ReturnCashTajmi.Amount AS ReturnCashTajmiAmount,
    ReturnCashTajmi.Cnt AS ReturnCashTajmi
FROM DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNContract C WITH (NOLOCK) 
    ON G.ContractRef = C.Serial
INNER JOIN Leasing.dbo.BASProtocolDetail BPD WITH (NOLOCK) 
    ON C.PrtDetailRef = BPD.Serial
INNER JOIN Leasing.dbo.BASProtocol BP WITH (NOLOCK) 
    ON BPD.ProtocolRef = BP.Serial
LEFT JOIN Leasing.dbo.[Installment#] AS [I#] WITH (NOLOCK) 
    ON C.Serial = [I#].ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef,
           ISNULL(P.Debt, 0) + ISNULL(P.Profit, 0) + ISNULL(P.Insurance, 0) + ISNULL(P.LackSurplus, 0) AS Ghest
    FROM Leasing.dbo.FUNPayment P WITH (NOLOCK)
    WHERE [Row] = 2
) P2 ON C.Serial = P2.ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(N.Amount) AS Amount
    FROM Leasing.dbo.ACCImport I WITH (NOLOCK)
    INNER JOIN Leasing.dbo.FUNConnector N WITH (NOLOCK) 
        ON I.Serial = N.ACCImportRef
    GROUP BY ContractRef
) I ON C.Serial = I.ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(Amount) AS Amount
    FROM Leasing.dbo.ACCImport I WITH (NOLOCK)
    WHERE History = 2
    GROUP BY ContractRef
) H ON C.Serial = H.ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(Amount) AS Amount
    FROM Leasing.dbo.FUNCash WITH (NOLOCK)
    WHERE DC = 0 AND BehalfRef = 53
    GROUP BY ContractRef
) Esterdad ON C.Serial = Esterdad.ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(Amount) AS Amount
    FROM Leasing.dbo.FUNCash WITH (NOLOCK)
    WHERE DC = 1 AND BehalfRef = 94 AND Active = 1
      AND (([Type] = 13 AND TreStatus = 3) OR ([Type] = 2 AND TreStatus IN (0, 3)))
    GROUP BY ContractRef
) S ON C.Serial = S.ContractRef
Left Outer Join (
    SELECT P.ContractRef, Sum(ISNULL(P.Debt, 0) + ISNULL(P.Profit, 0) + ISNULL(P.Insurance, 0) + ISNULL(P.LackSurplus, 0)) AS Ghest
    FROM Leasing.dbo.FUNPayment P WITH (NOLOCK)
    WHERE  P.ShouldPayDate <= @TodayDate
    Group BY P.ContractRef
) POld ON C.Serial = POld.ContractRef
Left Outer Join (
    SELECT ContractRef, Sum(ISNULL(P.Debt, 0) + ISNULL(P.Profit, 0) + ISNULL(P.Insurance, 0) + ISNULL(P.LackSurplus, 0)) AS Ghest
    FROM Leasing.dbo.FUNPayment P WITH (NOLOCK)
    Where P.ShouldPayDate > @TodayDate
    Group BY P.ContractRef
) PAti ON C.Serial = PAti.ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(Amount) AS Amount, COUNT(*) AS Cnt
    FROM Leasing.dbo.FUNCash WITH (NOLOCK)
    WHERE DC = 1 AND BehalfRef = 94 AND TreStatus = 5
      AND Dsr NOT LIKE N'%تجميع%'
    GROUP BY ContractRef
) ReturnCash ON C.Serial = ReturnCash.ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(Amount) AS Amount, COUNT(*) AS Cnt
    FROM Leasing.dbo.FUNCash WITH (NOLOCK)
    WHERE DC = 1 AND BehalfRef = 94 AND TreStatus = 5
      AND Dsr LIKE N'%تجميع%'
    GROUP BY ContractRef
) ReturnCashTajmi ON C.Serial = ReturnCashTajmi.ContractRef
--INNER JOIN Leasing.dbo.[Clear#] L WITH (NOLOCK) 
--    ON C.Serial = L.ContractRef
WHERE C.Serial = @ContractRef;
";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<ComprehensivePayDetailItem>(sql, new
        {
            ContractRef = contractRef
        });

        return result.ToList();
    }

    public async Task<List<SettlementDetailItem>> GetSettlementDetailAsync(long contractRef)
    {
        const string sql = @"
DECLARE @TodayDate CHAR(8) = LEFT(FORMAT(GETDATE(), 'yyyyMMdd', 'fa-ir'), 8);

-- اجرای SP_Clear
EXEC Leasing.dbo.SP_Clear @ContractRef, @TodayDate, '', 1, @TodayDate, 'Staff', 1, 0;

SELECT 
    C.ContractNo,
    C.Serial AS ContractSerial,
    L.ForFeit AS Penalty,
    L.ClearAmount AS ClearAmount,
    CASE 
        WHEN C.Status = 11 THEN 0 
        ELSE Leasing.dbo.Calc_Forfeit(
            L.ForFeit - ISNULL(S.Amount, 0) + CASE WHEN G.Moavagh < 0 THEN G.Moavagh ELSE 0 END,
            BP.ForfeitRate, BP.ForfeitRateConst, BP.ForfeitBacking,
            BP.SoonPayRate, BP.SoonPayRateConst, BP.SoonPayFloor, BP.SoonPayCeiling,
            Leasing.dbo.Calc_diff_of_dates(@TodayDate, [I#].LastInstallmentDate, 1),
            @TodayDate,
            [I#].LastInstallmentDate, [I#].Rate, 1, [I#].SumDebt, BP.ForfeitRateCoef
        ) 
    END AS UnpaidPenaltyAmount,
    L.SumWageAmount AS KarmozdVosoolMotalebat,
    isnull( L.InsBody,0) + isnull( L.SumInsPostScript,0) AS InsuranceIssuedAmount
FROM DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNContract C WITH (NOLOCK) 
    ON G.ContractRef = C.Serial
INNER JOIN Leasing.dbo.BASProtocolDetail BPD WITH (NOLOCK) 
    ON C.PrtDetailRef = BPD.Serial
INNER JOIN Leasing.dbo.BASProtocol BP WITH (NOLOCK) 
    ON BPD.ProtocolRef = BP.Serial
LEFT JOIN Leasing.dbo.[Installment#] AS [I#] WITH (NOLOCK) 
    ON C.Serial = [I#].ContractRef
LEFT OUTER JOIN (
    SELECT ContractRef, SUM(Amount) AS Amount
    FROM Leasing.dbo.FUNCash WITH (NOLOCK)
    WHERE DC = 1 AND BehalfRef = 94 AND Active = 1
      AND (([Type] = 13 AND TreStatus = 3) OR ([Type] = 2 AND TreStatus IN (0, 3)))
    GROUP BY ContractRef
) S ON C.Serial = S.ContractRef
INNER JOIN Leasing.dbo.[Clear#] L WITH (NOLOCK) 
    ON C.Serial = L.ContractRef
WHERE C.Serial = @ContractRef;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<SettlementDetailItem>(
            sql,
            new { ContractRef = contractRef });

        var list = result.ToList();

        // ═══════════════════════════════════════════════════════════
        //  محاسبه نهایى
        // ═══════════════════════════════════════════════════════════
        foreach (var item in list)
        {
            if (item.UnpaidPenaltyAmount < 0)
                item.UnpaidPenaltyAmount = 0;

            item.FinalSettlementAmount =
                (item.UnpaidPenaltyAmount ?? 0m) + (item.ClearAmount ?? 0m);
        }

        return list;
    }    //  SendSms — پیامک‌های ارسالى
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<SendSmsItem>> GetSendSmsAsync(long contractRef)
    {
        const string sql = @"
SELECT 
    ROW_NUMBER() OVER (ORDER BY s.Serial DESC) AS RowNumber,
    s.Sms_Text  AS SmsText,
    CAST(s.senddate AS NVARCHAR(20)) AS SendDate,
    CAST(s.SendTime AS NVARCHAR(20)) AS SendTime,
    s.Mobile    AS Mobile
FROM [Leasing].dbo.FUNCustomer CU WITH (NOLOCK)
INNER JOIN [Leasing].dbo.FunCustomerContacts cc WITH (NOLOCK) 
    ON cc.CifRef = cu.Serial
INNER JOIN [Leasing].dbo.funsms s WITH (NOLOCK) 
    ON s.mobile = cc.mobile1
WHERE s.ContractRef = @ContractRef
ORDER BY s.Serial DESC;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<SendSmsItem>(sql, new { ContractRef = contractRef });
        return result.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ReceiveSms — پیامک‌های دریافتى
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<ReceiveSmsItem>> GetReceiveSmsAsync(long contractRef)
    {
        const string sql = @"
DECLARE @ContractNo NVARCHAR(50);

SELECT @ContractNo = C.ContractNo
FROM [Leasing].dbo.FUNContract C WITH (NOLOCK)
WHERE C.Serial = @ContractRef;

IF @ContractNo IS NULL
BEGIN
    SELECT TOP 0 
        CAST(NULL AS BIGINT)       AS RowNumber,
        CAST(NULL AS NVARCHAR(50)) AS MobileSender,
        CAST(NULL AS NVARCHAR(20)) AS SendDate,
        CAST(NULL AS NVARCHAR(20)) AS SendTime,
        CAST(NULL AS NVARCHAR(MAX)) AS MessageText;
    RETURN;
END

SELECT 
    ROW_NUMBER() OVER (ORDER BY SendDate DESC) AS RowNumber,
    [MobileSender]                              AS MobileSender,
    CAST([SendDate] AS NVARCHAR(20))            AS SendDate,
    CAST([SendTime] AS NVARCHAR(20))            AS SendTime,
    [MessageText]                               AS MessageText
FROM [DarikAssistant].[gen].[SmsReport]
WHERE ContractNo = @ContractNo
ORDER BY SendDate DESC;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<ReceiveSmsItem>(sql, new { ContractRef = contractRef });
        return result.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Deposit — واریزى / چک / استرداد
    // ═══════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════
    //  Deposit — واریزى / استرداد
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<DepositItem>> GetDepositAsync(long contractRef)
    {
        const string sql = @"
SELECT 
    ROW_NUMBER() OVER (PARTITION BY ContractRef ORDER BY NoteDate, NoteNo) AS RowNumber,
    FORMAT(NoteDate, '####/##/##')  AS NoteDate,
    NoteNo                          AS NoteNo,
    FORMAT(Amount, '###,###')       AS Amount,
    Title                           AS Title,
    Dsr                             AS Dsr,
    BankTitle                       AS BankTitle,
    BankAccNo                       AS BankAccNo,
    CashType                        AS CashType
FROM (
    -- واریزها
    SELECT 
        L.Title, 
        I.NoteDate, 
        I.NoteNo, 
        I.Amount, 
        I.ContractRef, 
        I.Dsr AS Dsr, 
        LS.Title AS CashType,
        I.ACCNo  AS BankAccNo, 
        ac.Title AS BankTitle
    FROM [Leasing].[dbo].ACCImport I WITH (NOLOCK)
    INNER JOIN [Leasing].[dbo].FUNContract C WITH (NOLOCK) 
        ON I.ContractRef = C.Serial
    INNER JOIN [Leasing].[dbo].BASLookup L WITH (NOLOCK) 
        ON I.History = L.Code AND L.Type = 'HISTORY'
    LEFT JOIN [Leasing].[dbo].accdl ac WITH (NOLOCK) 
        ON ac.Serial = I.DLRef
    LEFT OUTER JOIN [Leasing].[dbo].FUNCash S WITH (NOLOCK) 
        ON I.ParentCashRef = S.Serial
    LEFT OUTER JOIN [Leasing].[dbo].BASLookup LS WITH (NOLOCK) 
        ON S.Type = LS.Code AND LS.Type = 'cashType'
    WHERE I.ContractRef = @ContractRef

    UNION ALL

    -- استردادها
    SELECT 
        L.Title, 
        S.NoteDate, 
        S.NoteNo, 
        S.Amount * -1, 
        S.ContractRef, 
        S.Dsr AS Dsr,
        N'چک' AS CashType,
        ac.Aux   AS BankAccNo,
        ac.Title AS BankTitle
    FROM [Leasing].[dbo].FUNCash S WITH (NOLOCK)
    INNER JOIN [Leasing].[dbo].BASLookup L WITH (NOLOCK) 
        ON S.BehalfRef = L.Code AND L.Type = 'behalf'
    LEFT JOIN [Leasing].[dbo].accdl ac WITH (NOLOCK) 
        ON ac.Serial = S.BankDLRef
    WHERE S.Dc = 0 
      AND S.BehalfRef = 53
      AND S.ContractRef = @ContractRef
) T
ORDER BY RowNumber;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<DepositItem>(
            sql,
            new { ContractRef = contractRef });

        return result.ToList();
    }
    // ═══════════════════════════════════════════════════════════════
    //  FunPayment — اقساط
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<FunPaymentItem>> GetFunPaymentAsync(long contractRef)
    {
        const string sql = @"
SELECT 
    P.Row AS RowNumber,
    CASE 
        WHEN P.ShouldPayDate IS NOT NULL AND P.ShouldPayDate != ''
        THEN FORMAT(P.ShouldPayDate, '####/##/##') 
        ELSE '' 
    END AS ShouldPayDate,
    FORMAT((P.Debt + P.Insurance + P.Profit + P.LackSurplus), '###,###') AS InstallmentAmount,
    P.Pay AS PayAmount,
    CASE 
        WHEN P.PayDate IS NOT NULL AND P.PayDate != ''
        THEN FORMAT(P.PayDate, '####/##/##') 
        ELSE '' 
    END AS PayDate,
    P.Creditor AS Creditor,
    CASE 
        WHEN ISNULL(P.Pay, 0) = 0 THEN 'red'
        WHEN ISNULL(P.Pay, 0) >= (P.Debt + P.Insurance + P.Profit + P.LackSurplus) THEN 'green'
        ELSE 'yellow'
    END AS Color
FROM [Leasing].[dbo].FUNPayment P WITH (NOLOCK)
WHERE P.ContractRef = @ContractRef
ORDER BY P.ShouldPayDate;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<FunPaymentItem>(sql, new { ContractRef = contractRef });
        return result.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sponser — راهبر / تامین‌کننده مالى
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<SponserItem>> GetSponserAsync(string contractNo)
    {
        const string sql = @"
SELECT 
    [تامين کننده مالي]                AS SponserName,
    [تعداد اقساط معوق]                AS ArrearsInstallmentCount,
    [مجموع مبلغ اقساط معوق]           AS TotalArrearsAmount,
     DateUpdate                        AS UpdateDate,
    TimeUpdate                        AS UpdateTime
FROM [ImportedFiles].dbo.Rahbar_Arrears WITH (NOLOCK)
WHERE contractNo = @ContractNo




";
       

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<SponserItem>(sql, new { ContractNo = contractNo });
        return result.ToList();
    }
    // ═══════════════════════════════════════════════════════════════
    //  Treasury — خزانه
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<TreasuryItem>> GetTreasuryAsync(long contractRef)
    {
        const string sql = @"
SELECT 
    ROW_NUMBER() OVER (ORDER BY NoteDate, NoteNo) AS RowNumber,
    L.Title         AS Title,
    FORMAT(NoteDate, '####/##/##') AS NoteDate,
    NoteNo          AS NoteNo,
    SayadNoteNo     AS SayadNoteNo,
    Amount          AS Amount,
    ac.Title        AS BankTitle,
    ac.Aux          AS BankAccNo
FROM [Leasing].[dbo].FUNCash S WITH (NOLOCK)
INNER JOIN [Leasing].[dbo].BASLookup L WITH (NOLOCK) 
    ON S.Type = L.Code AND L.Type = 'cashtype'
LEFT JOIN [Leasing].[dbo].accdl ac WITH (NOLOCK) 
    ON ac.Serial = S.BankDLRef
WHERE S.active = 1 
  AND S.BehalfRef = 94
  AND S.ContractRef = @ContractRef
  AND (
        (S.Type = 13 AND S.TreStatus = 3) 
        OR 
        (S.Type = 2  AND S.TreStatus IN (0, 3))
      )

UNION ALL

SELECT 
    ROW_NUMBER() OVER (ORDER BY NoteDate, NoteNo) AS RowNumber,
    L.Title         AS Title,
    FORMAT(NoteDate, '####/##/##') AS NoteDate,
    NoteNo          AS NoteNo,
    SayadNoteNo     AS SayadNoteNo,
    Amount          AS Amount,
    ac.Title        AS BankTitle,
    ac.Aux          AS BankAccNo
FROM [Leasing].[dbo].FUNCash S WITH (NOLOCK)
INNER JOIN [Leasing].[dbo].BASLookup L WITH (NOLOCK) 
    ON S.Type = L.Code AND L.Type = 'cashtype'
LEFT JOIN [Leasing].[dbo].accdl ac WITH (NOLOCK) 
    ON ac.Serial = S.BankDLRef
WHERE S.BehalfRef = 94
  AND S.ContractRef = @ContractRef
  AND (
        (S.Type = 13 AND S.TreStatus = 3) 
        OR 
        (S.Type = 2  AND S.TreStatus IN (0, 3))
      )
  AND S.Serial NOT IN (
        SELECT FUNCashRef 
        FROM [Leasing].[dbo].FUNConnector WITH (NOLOCK) 
        WHERE FUNCashRef IS NOT NULL
      )
ORDER BY RowNumber;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<TreasuryItem>(
            sql,
            new { ContractRef = contractRef });
        return result.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Checks — چک‌ها
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<ChecksItem>> GetChecksAsync(long contractRef)
    {
        const string sql = @"
SELECT 
    ROW_NUMBER() OVER (ORDER BY NoteDate, NoteNo) AS RowNumber,
    L.Title         AS CheckKind,
    LB.Title        AS Behalf,
    FORMAT(NoteDate, '####/##/##') AS NoteDate,
    NoteNo          AS NoteNo,
    SayadNoteNo     AS SayadNoteNo,
    Amount          AS Amount,
    LS.Title        AS CheckStatus,
    S.Dsr           AS Dsr,
    CONCAT(D.AccNum, ' ', D.Title) AS BankName
FROM [Leasing].[dbo].FUNCash S WITH (NOLOCK)
INNER JOIN [Leasing].[dbo].BASLookup L WITH (NOLOCK)
    ON S.Type = L.Code AND L.Type = 'cashtype'
INNER JOIN [Leasing].[dbo].BASLookup LS WITH (NOLOCK)
    ON S.TreStatus = LS.Code AND LS.Type = 'chequestatus'
INNER JOIN [Leasing].[dbo].BASLookup LB WITH (NOLOCK)
    ON S.BehalfRef = LB.Code AND LB.Type = 'behalf'
LEFT OUTER JOIN [Leasing].[dbo].Accdl D WITH (NOLOCK)
    ON S.BankDLRef = D.Serial
WHERE S.[Type] IN (13, 4) 
  AND S.DC = 1
  AND S.ContractRef = @ContractRef
ORDER BY RowNumber;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<ChecksItem>(
            sql,
            new { ContractRef = contractRef });

        return result.ToList();
    }
    // ═══════════════════════════════════════════════════════════════
    //  Alarms — هشدارها
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<AlarmsItem>> GetAlarmsAsync(string contractNo)
    {
        const string sql = @"
SELECT 
    ROW_NUMBER() OVER (ORDER BY A.AlarmDate DESC, A.AlarmTime DESC) AS RowNumber,
    BASLookup_1.Title   AS AlarmTypeTitle,
    A.AlarmDsr          AS AlarmDsr,
    A.AlarmDate         AS AlarmDate,
    A.AlarmTime         AS AlarmTime,
    A.ContactNumber     AS ContactNumber,
    BASLookup_2.Title   AS AlarmStatusTitle,
    U.UserName          AS UserName
FROM Leasing.dbo.FUNCuAlarm AS A WITH (NOLOCK)
INNER JOIN Leasing.dbo.BASLookup AS BASLookup_1 WITH (NOLOCK)
    ON A.AlarmType = BASLookup_1.Code
LEFT OUTER JOIN Leasing.dbo.FUNCustomer AS FUNCustomer_1 WITH (NOLOCK)
    LEFT OUTER JOIN Leasing.dbo.FUNContract AS C WITH (NOLOCK)
        ON FUNCustomer_1.Serial = C.CifRef
    ON A.ContractRef = C.Serial
LEFT OUTER JOIN Leasing.dbo.UGSInfo AS U WITH (NOLOCK)
    ON A.SenderRef = U.Serial
LEFT OUTER JOIN Leasing.dbo.BASLookup AS BASLookup_2 WITH (NOLOCK)
    ON A.AlarmStatus = BASLookup_2.Code
LEFT OUTER JOIN Leasing.dbo.FUNCustomer AS FUNCustomer_2 WITH (NOLOCK)
    ON A.CifRef = FUNCustomer_2.Serial
LEFT OUTER JOIN (
    SELECT 
        CifRef,
        CASE 
            WHEN ISNULL(HomeTel1, '') <> '' THEN HomeTel1
            WHEN ISNULL(HomeTel2, '') <> '' THEN HomeTel2
            ELSE ''
        END AS HomeTel
    FROM Leasing.dbo.FunCustomerContacts WITH (NOLOCK)
    WHERE ConcatType = 2431
) AS FunCustomerContacts
    ON FUNCustomer_1.Serial = FunCustomerContacts.CifRef
LEFT OUTER JOIN Leasing.dbo.[ContractStatus#] AS CS WITH (NOLOCK)
    ON C.Serial = CS.ContractRef
LEFT OUTER JOIN Leasing.dbo.ACCDL AS ACCDL WITH (NOLOCK)
    ON A.PursuerRef = ACCDL.Serial
WHERE BASLookup_2.Type = 'AlarmStatus'
  AND BASLookup_1.Type = 'AlarmType'
  AND C.ContractNo = @ContractNo
ORDER BY RowNumber;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<AlarmsItem>(sql, new
        {
            ContractNo = contractNo
        });

        return result.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Insert Alarm — ثبت هشدار جدید
    // ═══════════════════════════════════════════════════════════════
    public async Task<InsertAlarmResult> InsertAlarmAsync(
        long userId,
        long contractSerial,
        string message,
        string contactNumber)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // ═══════════════════════════════════════════════════════════
        //  ۱. دریافت DarikUserRef از جدول app.Users
        // ═══════════════════════════════════════════════════════════
        const string userSql = @"
SELECT [DarikUserRef]
FROM [DarikAssistant].[app].[Users]
WHERE Id = @UserId;";

        var darikUserRef = await connection.QueryFirstOrDefaultAsync<int?>(
            userSql,
            new { UserId = userId });

        if (darikUserRef == null || darikUserRef <= 0)
        {
            return new InsertAlarmResult
            {
                IsSuccess = false,
                Message = "برای شما DarikUserRef ثبت نشده است. لطفاً با واحد IT تماس بگیرید."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۲. بررسی وجود قرارداد با این Serial
        // ═══════════════════════════════════════════════════════════
        const string checkContractSql = @"
SELECT COUNT(1)
FROM Leasing.dbo.FUNContract C WITH (NOLOCK)
WHERE C.Serial = @ContractSerial;";

        var contractExists = await connection.ExecuteScalarAsync<int>(
            checkContractSql,
            new { ContractSerial = contractSerial });

        if (contractExists <= 0)
        {
            return new InsertAlarmResult
            {
                IsSuccess = false,
                Message = "قراردادی با این شناسه یافت نشد."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۳. درج پیام جدید (پارامترى — ضد SQL Injection)
        // ═══════════════════════════════════════════════════════════
        const string insertSql = @"
INSERT INTO Leasing.dbo.FUNCuAlarm 
(
    CifRef,
    ContractRef,
    RegisterDate,
    AlarmDate,
    AlarmTime,
    AlarmStatus,
    AlarmDsr,
    AlarmType,
    SenderRef,
    Ranking,
    Cost,
    UsanceDate,
    ContactNumber,
    PursuerRef,
    FutureAct,
    CntSuspendAlarm
)
SELECT 
    NULL                                                AS CifRef,
    @ContractSerial                                    AS ContractRef,
    CAST(FORMAT(GETDATE(), 'yyyyMMdd', 'fa-IR') AS INT) AS RegisterDate,
    CAST(FORMAT(GETDATE(), 'yyyyMMdd', 'fa-IR') AS INT) AS AlarmDate,
    FORMAT(GETDATE(), 'HH:mm:ss')                       AS AlarmTime,
    1                                                   AS AlarmStatus,
    @Message                                            AS AlarmDsr,
    98                                                  AS AlarmType,
    @DarikUserRef                                       AS SenderRef,
    0                                                   AS Ranking,
    0                                                   AS Cost,
    0                                                   AS UsanceDate,
    @ContactNumber                                      AS ContactNumber,
    NULL                                                AS PursuerRef,
    ''                                                  AS FutureAct,
    GP.CntSuspend                                       AS CntSuspendAlarm
FROM (SELECT @ContractSerial AS ContractRef) AS D
LEFT OUTER JOIN Leasing.dbo.GeneralPayment# AS GP WITH (NOLOCK)
    ON GP.ContractRef = D.ContractRef;";

        try
        {
            var affected = await connection.ExecuteAsync(insertSql, new
            {
                ContractSerial = contractSerial,
                Message = message,
                DarikUserRef = darikUserRef.Value,
                ContactNumber = contactNumber
            });

            if (affected <= 0)
            {
                return new InsertAlarmResult
                {
                    IsSuccess = false,
                    Message = "درج پیام انجام نشد."
                };
            }

            return new InsertAlarmResult
            {
                IsSuccess = true,
                Message = "پیام با موفقیت ثبت شد."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در درج پیام برای قرارداد {ContractSerial}", contractSerial);
            return new InsertAlarmResult
            {
                IsSuccess = false,
                Message = "خطا در ثبت پیام در دیتابیس."
            };
        }
    }


    // ═══════════════════════════════════════════════════════════════
    //  Update Contacts — به‌روزرسانی شماره تماس مشتری
    // ═══════════════════════════════════════════════════════════════
    public async Task<UpdateContactsResult> UpdateContactsAsync(
        string contractNo,
        string? mobile1,
        string? mobile2)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // ═══════════════════════════════════════════════════════════
        //  بررسی وجود قرارداد
        // ═══════════════════════════════════════════════════════════
        const string checkSql = @"
SELECT COUNT(1)
FROM Leasing.dbo.FUNContract C WITH (NOLOCK)
WHERE C.ContractNo = @ContractNo;";

        var contractExists = await connection.ExecuteScalarAsync<int>(
            checkSql,
            new { ContractNo = contractNo });

        if (contractExists <= 0)
        {
            return new UpdateContactsResult
            {
                IsSuccess = false,
                Message = "قراردادی با این شماره یافت نشد."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  به‌روزرسانی (فقط فیلدهای ارسال‌شده)
        // ═══════════════════════════════════════════════════════════
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("ContractNo", contractNo);

        if (mobile1 != null)
        {
            setClauses.Add("cc.Mobile1 = @Mobile1");
            parameters.Add("Mobile1", mobile1);
        }

        if (mobile2 != null)
        {
            setClauses.Add("cc.Mobile2 = @Mobile2");
            parameters.Add("Mobile2", mobile2);
        }

        if (setClauses.Count == 0)
        {
            return new UpdateContactsResult
            {
                IsSuccess = false,
                Message = "حداقل یکی از شماره‌ها باید ارسال شود."
            };
        }

        var updateSql = $@"
UPDATE cc
SET {string.Join(", ", setClauses)}
FROM Leasing.dbo.FUNContract C WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNCustomer CU WITH (NOLOCK) 
    ON CU.Serial = C.CifRef
INNER JOIN Leasing.dbo.FunCustomerContacts cc WITH (NOLOCK) 
    ON cc.DefaultAddress = 1 
   AND cc.CifRef = CU.Serial
WHERE C.ContractNo = @ContractNo;";

        try
        {
            var affected = await connection.ExecuteAsync(updateSql, parameters);

            if (affected <= 0)
            {
                return new UpdateContactsResult
                {
                    IsSuccess = false,
                    Message = "به‌روزرسانی انجام نشد. ممکن است آدرس پیش‌فرض ثبت نشده باشد."
                };
            }

            return new UpdateContactsResult
            {
                IsSuccess = true,
                Message = "شماره تماس با موفقیت به‌روزرسانی شد.",
                AffectedRows = affected
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در به‌روزرسانی شماره تماس برای قرارداد {ContractNo}", contractNo);
            return new UpdateContactsResult
            {
                IsSuccess = false,
                Message = "خطا در به‌روزرسانی دیتابیس."
            };
        }
    }
    ////////////////////////////
    ///

    // ═══════════════════════════════════════════════════════════════
    //  Update Rahbar — استعلام از بانک + به‌روزرسانی Rahbar_Arrears
    // ═══════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════
    //  Update Rahbar — استعلام + DELETE + INSERT
    // ═══════════════════════════════════════════════════════════════
    public async Task<UpdateRahbarResult> UpdateRahbarAsync(
        string contractNo,
        string installmentNo)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // ═══════════════════════════════════════════════════════════
        //  ۱. دریافت NationalCode از ContractNo
        // ═══════════════════════════════════════════════════════════
        const string lookupSql = @"
SELECT TOP 1 CU.NationalCode
FROM Leasing.dbo.FUNContract C WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNCustomer CU WITH (NOLOCK) 
    ON CU.Serial = C.CifRef
WHERE C.ContractNo = @ContractNo;";

        var nationalCode = await connection.QueryFirstOrDefaultAsync<string>(
            lookupSql, new { ContractNo = contractNo });

        if (string.IsNullOrWhiteSpace(nationalCode))
        {
            return new UpdateRahbarResult
            {
                IsSuccess = false,
                Message = "قراردادی با این شماره یافت نشد."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۲. استعلام از API بانک
        // ═══════════════════════════════════════════════════════════
        var (apiSuccess, apiMessage, installments) =
     await _rahbarService.CallInquiryApiAsync(nationalCode, installmentNo);

        if (!apiSuccess)
        {
            return new UpdateRahbarResult
            {
                IsSuccess = false,
                Message = apiMessage ?? "خطا در استعلام از راهبر"
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۳. محاسبه مجموع مبلغ اقساط سررسید‌شده
        // ═══════════════════════════════════════════════════════════
        var today = DateTime.Today;
        long totalPayableAmount = 0;
        int arrearsCount = 0;

        foreach (var inst in installments)
        {
            if (inst.DueDate.HasValue && inst.DueDate.Value.Date <= today)
            {
                totalPayableAmount += inst.TotalPayableAmount;
                arrearsCount++;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ۴. DELETE + INSERT در Rahbar_Arrears
        // ═══════════════════════════════════════════════════════════
        try
        {
            const string updateSql = @"
DECLARE @CustomerName   NVARCHAR(500);
DECLARE @CustomerNC     NVARCHAR(500);
DECLARE @SponserName    NVARCHAR(500);

-- ۱. SELECT از Rahbar
SELECT TOP 1
    @CustomerName = CU.FName + ' ' + CU.LName,
    @CustomerNC   = CU.NationalCode,
    @SponserName  = IsNull(R.[تامين کننده مالي], ISNULL(A.[تامين کننده مالي], D.Title))
	FROM Leasing..FunContract C WITH (NOLOCK)
	Inner Join Leasing..FunCustomer CU WITH (NOLOCK) ON C.CifRef = CU.Serial
	Left Outer Join ImportedFiles..rahbar R ON C.InstallmentNo = R.[شناسه تسهيلات]
	Left Outer Join ImportedFiles..Rahbar_Arrears A ON C.InstallmentNo = A.[شناسه تسهيلات]
	Inner Join Leasing..BASProtocolDetail BPD ON C.PrtDetailRef = BPD.Serial
	Inner Join Leasing..BASProtocol BP ON BPD.ProtocolRef = BP.Serial
	Inner Join Leasing..ACCDl D ON BP.SourceRef = D.Serial
	WHERE InsTallmentNo = @InstallmentNo;

BEGIN TRANSACTION;
BEGIN TRY
    -- ۲. DELETE رکوردهاى قبلى
    DELETE FROM ImportedFiles..Rahbar_Arrears
    WHERE [شناسه تسهيلات] = @InstallmentNo
      AND ContractNo = @ContractNo;

    -- ۳. MAX(RowNumber) + 1
    DECLARE @MaxRow INT = ISNULL(
        (SELECT MAX(RowNumber) FROM ImportedFiles..Rahbar_Arrears WITH (NOLOCK)), 
        0);

    -- ۴. INSERT (همیشه)
    INSERT INTO ImportedFiles..Rahbar_Arrears
    (
        RowNumber,
        [نام مشتري],
        [کد ملي مشتري],
        [تامين کننده مالي],
        [شناسه تسهيلات],
        [تعداد اقساط معوق],
        [مجموع مبلغ اقساط معوق],
        DateUpdate,
        TimeUpdate,
        NationalCode,
        ContractNo
    )
    VALUES
    (
        @MaxRow + 1,
        @CustomerName,
        @CustomerNC,
        @SponserName,
        @InstallmentNo,
        ISNULL(@ArrearsCount, 0),
        ISNULL(@TotalArrearsAmount, 0),
        FORMAT(GETDATE(), 'yyyyMMdd', 'fa-ir'),
        FORMAT(GETDATE(), 'HH:mm:ss'),
        @CustomerNC,
        @ContractNo
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;";
            await connection.ExecuteAsync(updateSql, new
            {
                InstallmentNo = installmentNo,
                ContractNo = contractNo,
                ArrearsCount = arrearsCount,
                TotalArrearsAmount = totalPayableAmount
            });

            _logger.LogInformation(
                "Rahbar_Arrears updated: ContractNo={ContractNo}, InstallmentNo={InstallmentNo}, " +
                "ArrearsCount={Count}, Total={Total}",
                contractNo, installmentNo, arrearsCount, totalPayableAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "خطا در DELETE+INSERT Rahbar_Arrears برای ContractNo={ContractNo}, InstallmentNo={InstallmentNo}",
                contractNo, installmentNo);

            return new UpdateRahbarResult
            {
                IsSuccess = false,
                Message = $"خطا در ذخیره در دیتابیس: {ex.Message}",
                TotalPayableAmount = totalPayableAmount,
                ArrearsInstallmentCount = arrearsCount
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۵. پاسخ — فقط دو فیلد
        // ═══════════════════════════════════════════════════════════
        return new UpdateRahbarResult
        {
            IsSuccess = true,
            Message = arrearsCount > 0
                ? $"استعلام موفق — {arrearsCount} قسط سررسید — مجموع: {totalPayableAmount:N0} ریال"
                : "استعلام موفق — هیچ قسط سررسید‌شده‌ای وجود ندارد",
            TotalPayableAmount = totalPayableAmount,
            ArrearsInstallmentCount = arrearsCount
        };
    }
    // ═══════════════════════════════════════════════════════════════
    //  فراخوانى API استعلام (معادل Python `call_inquiry_api`)
    // ═══════════════════════════════════════════════════════════════
   
    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }


    // ═══════════════════════════════════════════════════════════════
    //  Contract Detail — جزئیات قرارداد (اصل، فرع، نرخ، مدت، اقساط)
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<ContractDetailItem>> GetContractDetailAsync(long contractRef)
    {
        const string sql = @"
SELECT 
    GP.PursuerName          AS PursuerName,
    I.SumDebt               AS SumDebt,
    I.Profit                AS Profit,
    I.Rate                  AS Rate,
    I.Duration              AS Duration,
    I.Step                  AS Step,
    I.Duration / I.Step     AS InstallmentCount
FROM Leasing.dbo.FUNContract C WITH (NOLOCK)
INNER JOIN Leasing.dbo.[Installment#] I WITH (NOLOCK) 
    ON C.Serial = I.ContractRef
INNER JOIN Leasing.dbo.GeneralPayment# GP WITH (NOLOCK) 
    ON C.Serial = GP.ContractRef
WHERE C.Serial = @ContractRef;";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<ContractDetailItem>(
            sql,
            new { ContractRef = contractRef });

        return result.ToList();
    }




    // ═══════════════════════════════════════════════════════════════
    //  Send Template SMS — جایگزینى placeholders در قالب
    // ═══════════════════════════════════════════════════════════════
    public async Task<SendTemplateSmsResult> SendTemplateSmsAsync(
     long templateMessageId,
     string contractNo,
     long userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // ═══════════════════════════════════════════════════════════
        //  ۱. DarikUserRef
        // ═══════════════════════════════════════════════════════════
        const string userSql = @"
SELECT [DarikUserRef]
FROM [DarikAssistant].[app].[Users]
WHERE Id = @UserId;";

        var darikUserRef = await connection.QueryFirstOrDefaultAsync<long?>(
            userSql,
            new { UserId = userId });

        if (darikUserRef == null || darikUserRef <= 0)
        {
            return new SendTemplateSmsResult
            {
                IsSuccess = false,
                Message = "برای شما DarikUserRef ثبت نشده است. لطفاً با واحد IT تماس بگیرید."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۲. اطلاعات قرارداد
        // ═══════════════════════════════════════════════════════════
        const string contractSql = @"
SELECT 
    CU.FName + N' ' + CU.LName AS FullName,
    CU.NationalCode             AS NationalCode,
    cc.Mobile1                  AS Mobile1,
    G.Tarh                      AS Tarh,
    C.ContractNo                AS ContractNo,
    C.Serial                    AS ContractRef,
    FORMAT(L.ForFeit, 'N0')     AS ForFeit,
    FORMAT(L.ClearAmount, 'N0') AS ClearAmount,
    FORMAT(G.Moavagh, 'N0')     AS Moavagh
FROM DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNContract C WITH (NOLOCK) 
    ON G.ContractRef = C.Serial
INNER JOIN Leasing.dbo.FUNCustomer CU WITH (NOLOCK) 
    ON C.CifRef = CU.Serial
INNER JOIN Leasing.dbo.[Clear#] L WITH (NOLOCK) 
    ON C.Serial = L.ContractRef
LEFT JOIN [Leasing].dbo.FunCustomerContacts cc WITH (NOLOCK) 
    ON cc.CifRef = CU.Serial AND cc.ConcatType = 2431
WHERE C.ContractNo = @ContractNo;";

        var contractData = await connection.QueryFirstOrDefaultAsync<dynamic>(
            contractSql,
            new { ContractNo = contractNo });

        if (contractData == null)
        {
            return new SendTemplateSmsResult
            {
                IsSuccess = false,
                Message = "قراردادی با این شماره یافت نشد."
            };
        }

        string? mobile = (string?)contractData.Mobile1;
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return new SendTemplateSmsResult
            {
                IsSuccess = false,
                Message = "شماره موبایل برای این قرارداد ثبت نشده است."
            };
        }
        //mobile = "09126766128";
        long contractRef = (long)contractData.ContractRef;

        // ═══════════════════════════════════════════════════════════
        //  ۳. قالب
        // ═══════════════════════════════════════════════════════════
        const string templateSql = @"
SELECT TOP 1 
    [ID]      AS Id,
    [Message] AS Message
FROM [DarikAssistant].[app].[MesssageTemplate] WITH (NOLOCK)
WHERE [ID] = @TemplateMessageId 
  AND [Active] = 1;";

        var template = await connection.QueryFirstOrDefaultAsync<dynamic>(
            templateSql,
            new { TemplateMessageId = templateMessageId });

        if (template == null)
        {
            return new SendTemplateSmsResult
            {
                IsSuccess = false,
                Message = "قالب پیام یافت نشد یا غیرفعال است."
            };
        }

        string? templateText = (string?)template.Message;
        if (string.IsNullOrWhiteSpace(templateText))
        {
            return new SendTemplateSmsResult
            {
                IsSuccess = false,
                Message = "متن قالب خالی است."
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ۴. جایگزینى placeholders
        // ═══════════════════════════════════════════════════════════
        var finalMessage = templateText
            .Replace("[FullName]", (string?)contractData.FullName ?? "")
            .Replace("[NationalCode]", (string?)contractData.NationalCode ?? "")
            .Replace("[Tarh]", (string?)contractData.Tarh ?? "")
            .Replace("[ContractNo]", (string?)contractData.ContractNo ?? "")
            .Replace("[ContractRef]", contractRef.ToString())
            .Replace("[ForFeit]", (string?)contractData.ForFeit ?? "0")
            .Replace("[ClearAmount]", (string?)contractData.ClearAmount ?? "0")
            .Replace("[Moavagh]", (string?)contractData.Moavagh ?? "0");

        // ═══════════════════════════════════════════════════════════
        //  ۵. تراکنش: FUNSms + SmsSend
        // ═══════════════════════════════════════════════════════════
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            // ─── ۵.۱. FUNSms ───
            const string insertFunSmsSql = @"
INSERT INTO Leasing.dbo.FUNSms
    (Mobile, Sms_Text, RegisterDate, RegisterTime, UserRef, ContractRef)
VALUES
    (@Mobile, @SmsText, 
     CAST(FORMAT(GETDATE(), 'yyyyMMdd', 'fa-ir') AS INT),
     FORMAT(GETDATE(), 'HH:mm', 'fa-ir'), 
     @UserRef, @ContractRef);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            var funSmsId = await connection.ExecuteScalarAsync<long>(
                insertFunSmsSql,
                new
                {
                    Mobile = mobile,
                    SmsText = finalMessage,
                    UserRef = darikUserRef.Value,
                    ContractRef = contractRef
                },
                transaction);

            // ─── ۵.۲. SmsSend ───
            const string insertSmsSendSql = @"
INSERT INTO [DarikAssistant].[gen].[SmsSend]
    (Sms_Text, Mobile, RegisterDate, RegisterTime, 
     ContractRef, DarikFunSmsRef, MessageTemplateRef, UserRef)
VALUES
    (@SmsText, @Mobile, 
     CAST(FORMAT(GETDATE(), 'yyyyMMdd', 'fa-ir') AS INT),
     FORMAT(GETDATE(), 'HH:mm', 'fa-ir'),
     @ContractRef, @DarikFunSmsRef, @MessageTemplateRef, @UserRef);";

            await connection.ExecuteAsync(
                insertSmsSendSql,
                new
                {
                    SmsText = finalMessage,
                    Mobile = mobile,
                    ContractRef = contractRef,
                    DarikFunSmsRef = funSmsId,
                    MessageTemplateRef = templateMessageId,
                    UserRef = darikUserRef.Value
                },
                transaction);

            await transaction.CommitAsync();

            return new SendTemplateSmsResult
            {
                IsSuccess = true,
                Message = "پیام با موفقیت ثبت شد.",
                FinalMessage = finalMessage
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "خطا در درج FUNSms/SmsSend برای TemplateMessageId={Id}, ContractNo={ContractNo}",
                templateMessageId, contractNo);

            return new SendTemplateSmsResult
            {
                IsSuccess = false,
                Message = $"خطا در ذخیره در دیتابیس: {ex.Message}"
            };
        }
    }
}