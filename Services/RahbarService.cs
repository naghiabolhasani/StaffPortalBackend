using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;

namespace StaffPortal.Services;

public class RahbarService : IRahbarService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RahbarService> _logger;
    private readonly string _connectionString;

    public RahbarService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RahbarService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  فراخوانى خام API استعلام (Golrang)
    // ═══════════════════════════════════════════════════════════════
   public async Task<(bool Success, string? Message, List<InquiryApiInstallment> Installments)>
    CallInquiryApiAsync(string nationalCode, string loanReferenceCode)
{
    var inquiryUrl = _configuration["GolrangBank:InquiryUrl"]
        ?? "https://orchestrator.golrangonline.ir/api/BankServices/InquiryPayableInstallments";
    var username = _configuration["GolrangBank:Username"] ?? "leasing";
    var password = _configuration["GolrangBank:Password"]
        ?? "v^z%ZDHzgWQ*K)kfLfu8z8(I3bnXHzMe";

    try
    {
        using var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // ─── Basic Auth ───
        var authBytes = System.Text.Encoding.UTF8.GetBytes($"{username}:{password}");
        var authBase64 = Convert.ToBase64String(authBytes);

        // ─── Payload ───
        var payload = new
        {
            customerNationalCode = nationalCode,
            loanReferenceCode = loanReferenceCode
        };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        // ─── Request ───
        using var request = new HttpRequestMessage(HttpMethod.Post, inquiryUrl);
        request.Headers.Add("accept", "text/plain");
        request.Headers.Add("User-Agent", "curl/7.68.0");
        request.Headers.Add("Authorization", $"Basic {authBase64}");
        request.Content = content;

        // ─── ارسال ───
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (false, $"HTTP {(int)response.StatusCode}", new List<InquiryApiInstallment>());

        // ─── پارس ───
        var apiResult = System.Text.Json.JsonSerializer.Deserialize<InquiryApiResponse>(
            responseBody,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (apiResult == null || !apiResult.Success)
        {
            var msg = apiResult?.Messages?.FirstOrDefault()?.Message ?? "پاسخ نامعتبر از بانک";
            return (false, msg, new List<InquiryApiInstallment>());
        }

        return (true, null, apiResult.Result ?? new List<InquiryApiInstallment>());
    }
    catch (TaskCanceledException)
    {
        return (false, "Timeout در ارتباط با بانک", new List<InquiryApiInstallment>());
    }
    catch (HttpRequestException ex)
    {
        return (false, $"خطای شبکه: {ex.Message}", new List<InquiryApiInstallment>());
    }
    catch (Exception ex)
    {
        return (false, $"خطای داخلی: {ex.Message}", new List<InquiryApiInstallment>());
    }
}
    // ═══════════════════════════════════════════════════════════════
    //  استعلام کامل با محاسبات
    // ═══════════════════════════════════════════════════════════════
    public async Task<RahbarInquiryResult> InquiryAsync(RahbarInquiryRequest request)
    {
        var result = new RahbarInquiryResult();

        // ─── اعتبارسنجى ───
        if (string.IsNullOrWhiteSpace(request.NationalCode))
        {
            result.IsSuccess = false;
            result.Message = "کد ملی اجباری است.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.LoanReferenceCode))
        {
            result.IsSuccess = false;
            result.Message = "شناسه تسهیلات اجباری است.";
            return result;
        }

        var nationalCode = request.NationalCode.Trim();
        var loanReferenceCode = request.LoanReferenceCode.Trim();

        // ─── فراخوانى API ───
        var (success, message, installments) =
            await CallInquiryApiAsync(nationalCode, loanReferenceCode);

        if (!success)
        {
            result.IsSuccess = false;
            result.Message = message ?? "خطا در استعلام از بانک";
            return result;
        }

        // ─── محاسبات ───
        var today = DateTime.Today;
        long totalPayable = 0;
        int dueCount = 0;

        foreach (var inst in installments)
        {
            var isDue = inst.DueDate.HasValue && inst.DueDate.Value.Date <= today;

            result.Installments.Add(new RahbarInstallmentItem
            {
                Number = inst.Number,
                Status = inst.Status,
                DueDate = inst.DueDate,
                InstallmentAmount = inst.InstallmentAmount,
                PenaltyAmount = inst.PenaltyAmount,
                TotalPayableAmount = inst.TotalPayableAmount,
                IsDue = isDue
            });

            if (isDue)
            {
                totalPayable += inst.TotalPayableAmount;
                dueCount++;
            }
        }

        result.IsSuccess = true;
        result.TotalInstallments = installments.Count;
        result.TotalPayableAmount = totalPayable;
        result.DueInstallments = dueCount;
        result.Message = dueCount > 0
            ? $"استعلام موفق — {dueCount} قسط سررسید — مجموع: {totalPayable:N0} ریال"
            : "استعلام موفق — هیچ قسط سررسید‌شده‌ای وجود ندارد";

        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper
    // ═══════════════════════════════════════════════════════════════
    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
    // ═══════════════════════════════════════════════════════════════
    //  قفل جلوگیرى از اجراى همزمان
    // ═══════════════════════════════════════════════════════════════
    private static readonly SemaphoreSlim _bulkRunLock = new(1, 1);
    private const int DelayBetweenCallsMs = 200;
    private const int MaxRetriesPerItem = 3;

    // ═══════════════════════════════════════════════════════════════
    //  اجراى گروهى استعلام
    // ═══════════════════════════════════════════════════════════════
    public async Task<RahbarBulkInquiryResult> RunBulkInquiryAsync(CancellationToken ct = default)
    {
        var result = new RahbarBulkInquiryResult { StartedAt = DateTime.Now };

        // ─── جلوگیرى از اجراى همزمان ───
        if (!await _bulkRunLock.WaitAsync(0, ct))
        {
            return new RahbarBulkInquiryResult
            {
                IsSuccess = false,
                Message = "یک استعلام گروهی دیگر در حال اجراست.",
                StartedAt = DateTime.Now
            };
        }

        try
        {
            _logger.LogInformation("=====================");
            _logger.LogInformation("Bulk inquiry started...");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

             //═══════════════════════════════════════════════════════
             // کوئرى با Dynamic SQL — اسم ستون‌ها را خودکار پیدا مى‌کند
             // (راه‌حل مشکل کاراکتر «ک» عربی/ فارسی)
             //═══════════════════════════════════════════════════════
                        const string listSql = @"
                                WITH Combined AS (
                SELECT 
                    r.[شناسه تسهيلات]   AS InstallmentNo, 
                    r.[کد ملي مشتري]    AS NationalCode,
                    '-1'                AS ArrearsCount
                FROM ImportedFiles..rahbar AS r WITH (NOLOCK)
                WHERE r.[شناسه تسهيلات] IS NOT NULL
                  AND NOT EXISTS (
                        SELECT 1 
                        FROM ImportedFiles..Rahbar_Arrears AS a WITH (NOLOCK)
                        WHERE a.[شناسه تسهيلات] = r.[شناسه تسهيلات]
                  )

                UNION ALL

                SELECT 
                    [شناسه تسهيلات]     AS InstallmentNo, 
                    [کد ملي مشتري]      AS NationalCode,
                    [تعداد اقساط معوق]  AS ArrearsCount
                FROM ImportedFiles..Rahbar_Arrears WITH (NOLOCK)
                WHERE [شناسه تسهيلات] IS NOT NULL
            )
            SELECT 
                InstallmentNo,
                NationalCode
            FROM Combined
            ORDER BY TRY_CAST(ArrearsCount AS INT) DESC,NationalCode ;";

            /*
            const string listSql = @"
                    WITH Combined AS (
    SELECT 
        r.[شناسه تسهيلات]   AS InstallmentNo, 
        r.[کد ملي مشتري]    AS NationalCode,
        '-1'                AS ArrearsCount
    FROM ImportedFiles..rahbar AS r WITH (NOLOCK)
    WHERE r.[شناسه تسهيلات] IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 
            FROM ImportedFiles..Rahbar_Arrears AS a WITH (NOLOCK)
            WHERE a.[شناسه تسهيلات] = r.[شناسه تسهيلات]
      )

    UNION ALL

    SELECT 
        [شناسه تسهيلات]     AS InstallmentNo, 
        [کد ملي مشتري]      AS NationalCode,
        [تعداد اقساط معوق]  AS ArrearsCount
    FROM ImportedFiles..Rahbar_Arrears WITH (NOLOCK)
    WHERE [شناسه تسهيلات] IS NOT NULL
)
SELECT 
    C.InstallmentNo,
    C.NationalCode
FROM Combined  C
left join [DarikAssistant].[gen].[RahbarInquiryPayableInstallments] r on r.InstallmentNo=c.InstallmentNo
where r.InstallmentNo is null or cast([InquiryDate] as date) <> '2026-11-02'

ORDER BY TRY_CAST(ArrearsCount AS INT) DESC,NationalCode ;";
*/

            var pairs = (await conn.QueryAsync<RahbarPair>(
                new CommandDefinition(listSql, cancellationToken: ct))).ToList();

            result.TotalProcessed = pairs.Count;
            _logger.LogInformation("Bulk inquiry: {Count} unique pairs to process.", pairs.Count);

            // ═══════════════════════════════════════════════════════
            //  حلقه روى هر جفت (بدون ذخیره در Details)
            // ═══════════════════════════════════════════════════════
            foreach (var pair in pairs)
            {
                if (ct.IsCancellationRequested) break;

                string? nc = pair.NationalCode?.Trim();
                string? instNo = pair.InstallmentNo?.Trim();

                if (string.IsNullOrWhiteSpace(nc) || string.IsNullOrWhiteSpace(instNo))
                    continue;

                try
                {
                    // ─── فراخوانى API با retry ───
                    bool success = false;
                    string? message = null;
                    List<InquiryApiInstallment>? installments = null;

                    for (int attempt = 1; attempt <= MaxRetriesPerItem; attempt++)
                    {
                        (success, message, installments) = await CallInquiryApiAsync(nc, instNo);
                        if (success) break;
                        if (attempt < MaxRetriesPerItem)
                            await Task.Delay(1000 * attempt, ct);
                    }

                    // ─── محاسبات ───
                    int? totalInst = null;
                    long? totalPayable = null;
                    int? dueCount = null;

                    if (success && installments != null)
                    {
                        var today = DateTime.Today;
                        totalInst = installments.Count;
                        dueCount = 0;
                        totalPayable = 0;

                        foreach (var inst in installments)
                        {
                            if (inst.DueDate.HasValue && inst.DueDate.Value.Date <= today)
                            {
                                totalPayable += inst.TotalPayableAmount;
                                dueCount++;
                            }
                        }
                    }

                    // ─── Upsert در DB ───
                    var action = await UpsertRahbarInquiryAsync(
                        conn, nc, instNo,
                        success ? "true" : "false",
                        message ?? (success ? "OK" : "Unknown error"),
                        totalInst, totalPayable, dueCount, ct);

                    if (action == "Inserted") result.Inserted++;
                    else if (action == "Updated") result.Updated++;
                    else result.Failed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Bulk inquiry item failed: NC={NC}, InstNo={InstNo}", nc, instNo);
                    result.Failed++;
                }

                // ─── تأخیر کوچک بین فراخوانى‌ها ───
                await Task.Delay(DelayBetweenCallsMs, ct);
            }

            result.IsSuccess = true;
            result.Message = $"Processed: {result.TotalProcessed} | " +
                             $"Insert: {result.Inserted} | " +
                             $"Update: {result.Updated} | " +
                             $"Error: {result.Failed}";

            _logger.LogInformation("Bulk inquiry finished: {Message}", result.Message);
           

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk inquiry failed");
            result.IsSuccess = false;
            result.Message = $"خطا: {ex.Message}";
            return result;
        }
        finally
        {
            result.FinishedAt = DateTime.Now;
            _bulkRunLock.Release();
        }
    }
    // ═══════════════════════════════════════════════════════════════
    //  Upsert در gen.RahbarInquiryPayableInstallments
    // ═══════════════════════════════════════════════════════════════
    private async Task<string> UpsertRahbarInquiryAsync(
    SqlConnection conn,
    string nationalCode,
    string installmentNo,
    string isSuccess,
    string message,
    int? totalInst,
    long? totalPayable,
    int? dueCount,
    CancellationToken ct)
    {
        const string checkSql = @"
SELECT COUNT(1) 
FROM [DarikAssistant].[gen].[RahbarInquiryPayableInstallments] WITH (NOLOCK)
WHERE NationalCode = @NC AND InstallmentNo = @InstNo;";

        var exists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                checkSql,
                new { NC = nationalCode, InstNo = installmentNo },
                cancellationToken: ct));

        var truncatedMessage = Truncate(message, 200) ?? "";
        var truncatedNc = Truncate(nationalCode, 11) ?? "";
        var truncatedInstNo = Truncate(installmentNo, 30) ?? "";

        if (exists > 0)
        {
            const string updateSql = @"
UPDATE [DarikAssistant].[gen].[RahbarInquiryPayableInstallments]
SET IsSuccess = @IsSuccess,
    Message = @Message,
    TotalInstallments = @TotalInst,
    TotalPayableAmount = @TotalPayable,
    DueInstallments = @DueCount,
    InquiryDate = GETDATE()
WHERE NationalCode = @NC AND InstallmentNo = @InstNo;";

            await conn.ExecuteAsync(new CommandDefinition(updateSql, new
            {
                IsSuccess = isSuccess,
                Message = truncatedMessage,
                TotalInst = totalInst,
                TotalPayable = totalPayable,
                DueCount = dueCount,
                NC = truncatedNc,
                InstNo = truncatedInstNo
            }, cancellationToken: ct));

            return "Updated";
        }
        else
        {
            const string insertSql = @"
INSERT INTO [DarikAssistant].[gen].[RahbarInquiryPayableInstallments]
    (NationalCode, InstallmentNo, IsSuccess, Message, 
     TotalInstallments, TotalPayableAmount, DueInstallments, InquiryDate)
VALUES 
    (@NC, @InstNo, @IsSuccess, @Message, 
     @TotalInst, @TotalPayable, @DueCount, GETDATE());";

            await conn.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                NC = truncatedNc,
                InstNo = truncatedInstNo,
                IsSuccess = isSuccess,
                Message = truncatedMessage,
                TotalInst = totalInst,
                TotalPayable = totalPayable,
                DueCount = dueCount
            }, cancellationToken: ct));

            return "Inserted";
        }
    }

}