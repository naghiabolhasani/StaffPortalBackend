using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StaffPortal.Services;

public class RecieveSmsService : IStaffSmsService
{
    // ═══════════════════════════════════════════════════════════════
    //  ثابت‌ها
    // ═══════════════════════════════════════════════════════════════
    private const string SmsServiceUrl = "https://ws2.adpdigital.com/services/MessagingService";
    private const string SmsUserName = "arzesh-afarin";
    private const string SmsPassword = "Arzesh71";
    private const string SmsSenderNumber = "9820007171";
    private const string SmsReportTable = "[DarikAssistant].[gen].[SmsReport]";

    private const int BatchSize = 250;
    private const int MaxIterations = 200;
    private const int DelayBetweenCallsMs = 10000;

    // ═══════════════════════════════════════════════════════════════
    //  فیلدها
    // ═══════════════════════════════════════════════════════════════
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RecieveSmsService> _logger;
    private readonly string _connectionString;

    public RecieveSmsService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<RecieveSmsService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    // ═══════════════════════════════════════════════════════════════
    //  Throttle state (static تا بین requestها مشترک باشه)
    // ═══════════════════════════════════════════════════════════════
    private static DateTime _lastApiFetch = DateTime.MinValue;
    private static readonly SemaphoreSlim _apiFetchLock = new(1, 1);
    private const int MinSecondsBetweenApiFetches = 30;

    // ═══════════════════════════════════════════════════════════════
    //  دریافت از API با throttle (حداقل ۳۰ ثانیه بین fetchها)
    // ═══════════════════════════════════════════════════════════════
    private async Task TryFetchFromApiAsync()
    {
        // چک سریع قبل از گرفتن قفل
        if ((DateTime.UtcNow - _lastApiFetch).TotalSeconds < MinSecondsBetweenApiFetches)
            return;

        await _apiFetchLock.WaitAsync();
        try
        {
            // چک دوباره بعد از گرفتن قفل (شاید بین این مدت یک request دیگر fetch کرده باشد)
            if ((DateTime.UtcNow - _lastApiFetch).TotalSeconds < MinSecondsBetweenApiFetches)
                return;

            _logger.LogInformation("Throttled API fetch triggered by search.");

            var messages = await CallReceiveAsync(start: -1, maxSize: BatchSize);

            if (messages != null && messages.Count > 0)
            {
                // پارس همه پیام‌ها (بدون فیلتر تاریخ)
                var newRows = new List<dynamic>();

                foreach (var msg in messages)
                {
                    var timeStr = msg.Element("time")?.Value;
                    var apiId = msg.Element("id")?.Value;
                    var from = msg.Element("from")?.Value;
                    var to = msg.Element("to")?.Value;
                    var type = msg.Element("type")?.Value;
                    var encoding = msg.Element("encoding")?.Value;
                    var messageText = msg.Element("content")?.Value;
                    var msgStatus = msg.Element("status")?.Value;

                    if (string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(apiId))
                        continue;

                    if (!DateTime.TryParse(timeStr, null,
                        DateTimeStyles.RoundtripKind, out DateTime dt))
                        continue;

                    var (sendDateShamsi, sendTimeShamsi) = ConvertToShamsi(dt);

                    newRows.Add(new
                    {
                        ApiId = apiId,
                        MobileSender = from,
                        Reciever = to,
                        SendDate = sendDateShamsi,
                        SendTime = sendTimeShamsi,
                        MessageKind = MapMessageKind(type),
                        MessageCoding = MapMessageCoding(encoding),
                        MessageText = messageText,
                        MessageStatus = MapMessageStatus(msgStatus)
                    });
                }

                if (newRows.Count > 0)
                {
                    var inserted = await InsertNewRowsAsync(_connectionString, newRows);
                    _logger.LogInformation(
                        "Throttled fetch: {Fetched} messages, {Inserted} new inserted.",
                        messages.Count, inserted);
                }
            }

            _lastApiFetch = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API fetch failed, will use DB only");
            // _lastApiFetch آپدیت نمى‌شود تا در درخواست بعدى دوباره تلاش شود
        }
        finally
        {
            _apiFetchLock.Release();
        }
    }
    // ═══════════════════════════════════════════════════════════════
    //  جستجو با Fallback به API
    // ═══════════════════════════════════════════════════════════════
    public async Task<ReceiveSmsResult> ReceiveSmsSearchAsync(ReceiveSmsRequest request)
    {
        int pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        int pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        if (pageSize > 500) pageSize = 500;
        int offset = (pageNumber - 1) * pageSize;

        string? mobile = null;
        if (!string.IsNullOrWhiteSpace(request.Mobile))
            mobile = NormalizeMobileForSearch(request.Mobile);

        string fromDate = string.IsNullOrWhiteSpace(request.FromDate)
            ? "13000101"
            : request.FromDate.Trim();

        string toDate = string.IsNullOrWhiteSpace(request.ToDate)
            ? "14991229"
            : request.ToDate.Trim();

        // ═══════════════════════════════════════════════════════════
        //  ۱. تلاش براى دریافت پیام‌هاى جدید از API (throttle شده)
        // ═══════════════════════════════════════════════════════════
        await TryFetchFromApiAsync();

        // ═══════════════════════════════════════════════════════════
        //  ۲. جستجو در DB
        // ═══════════════════════════════════════════════════════════
        return await SearchInDbAsync(fromDate, toDate, mobile, pageNumber, pageSize, offset);
    }

    // ═══════════════════════════════════════════════════════════════
    //  جستجو در DB
    // ═══════════════════════════════════════════════════════════════
    private async Task<ReceiveSmsResult> SearchInDbAsync(
        string fromDate, string toDate, string? mobile,
        int pageNumber, int pageSize, int offset)
    {
        const string dataSql = @"
SELECT 
    S.[Id]                                          AS Id,
    S.[ApiId]                                       AS ApiId,
    STUFF(S.[MobileSender], 1, 2, '0')              AS Mobile,
    S.[MessageText]                                 AS Message,
    S.[SendDate]                                    AS SendDate,
    S.[SendTime]                                    AS SendTime,
    S.[CustomerName]                                AS CustomerName,
    S.[NationalCode]                                AS NationalCode,
    S.[ContractNo]                                  AS ContractNo,
    S.[LastPayDate]                                 AS LastPayDate,
    S.[Moavagh]                                     AS Moavagh,
    S.[Tarh]                                        AS Tarh,
    S.[IsAnswered]                                  AS IsAnswered,
    U.[Username]                                    AS AnsweredUserName,
    S.[AnsweredMessage]                             AS AnsweredMessage
FROM [DarikAssistant].[gen].[SmsReport] S WITH (NOLOCK)
LEFT JOIN [DarikAssistant].[app].[Users] U WITH (NOLOCK)
    ON U.[ID] = S.[AnsweredUserRef]
WHERE S.[SendDate] BETWEEN @FromDate AND @ToDate
  AND (S.[IsDeleted] = 0 OR S.[IsDeleted] IS NULL)
  AND (@Mobile IS NULL OR S.[MobileSender] LIKE '%' + @Mobile + '%')
ORDER BY S.[Id] DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        const string countSql = @"
SELECT COUNT(1)
FROM [DarikAssistant].[gen].[SmsReport] S WITH (NOLOCK)
WHERE S.[SendDate] BETWEEN @FromDate AND @ToDate
  AND (S.[IsDeleted] = 0 OR S.[IsDeleted] IS NULL)
  AND (@Mobile IS NULL OR S.[MobileSender] LIKE '%' + @Mobile + '%');";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var parameters = new
        {
            FromDate = fromDate,
            ToDate = toDate,
            Mobile = mobile,
            Offset = offset,
            PageSize = pageSize
        };

        var items = (await connection.QueryAsync<SmsReportItem>(dataSql, parameters)).ToList();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        return new ReceiveSmsResult
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  دریافت از API + درج (براى search وقتى DB خالى است)
    // ═══════════════════════════════════════════════════════════════
    private async Task<(int Fetched, int Inserted)> FetchFromApiAndInsertAsync(
        string fromDate, string toDate)
    {
        try
        {
            var messages = await CallReceiveAsync(start: -1, maxSize: 250);

            if (messages == null || messages.Count == 0)
                return (0, 0);

            int fromInt = int.Parse(fromDate);
            int toInt = int.Parse(toDate);

            var inRange = new List<dynamic>();

            foreach (var msg in messages)
            {
                var timeStr = msg.Element("time")?.Value;
                var apiId = msg.Element("id")?.Value;
                var from = msg.Element("from")?.Value;
                var to = msg.Element("to")?.Value;
                var type = msg.Element("type")?.Value;
                var encoding = msg.Element("encoding")?.Value;
                var messageText = msg.Element("content")?.Value;
                var msgStatus = msg.Element("status")?.Value;

                if (string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(apiId))
                    continue;

                if (!DateTime.TryParse(timeStr, null,
                    DateTimeStyles.RoundtripKind, out DateTime dt))
                    continue;

                var (sendDateShamsi, sendTimeShamsi) = ConvertToShamsi(dt);

                if (sendDateShamsi < fromInt || sendDateShamsi > toInt)
                    continue;

                inRange.Add(new
                {
                    ApiId = apiId,
                    MobileSender = from,
                    Reciever = to,
                    SendDate = sendDateShamsi,
                    SendTime = sendTimeShamsi,
                    MessageKind = MapMessageKind(type),
                    MessageCoding = MapMessageCoding(encoding),
                    MessageText = messageText,
                    MessageStatus = MapMessageStatus(msgStatus)
                });
            }

            if (inRange.Count == 0)
                return (messages.Count, 0);

            var inserted = await InsertNewRowsAsync(_connectionString, inRange);
            return (messages.Count, inserted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در FetchFromApiAndInsertAsync");
            return (0, 0);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  سینک دوره‌اى (Background Service)
    // ═══════════════════════════════════════════════════════════════
    private async Task SyncSmsFromWebServiceAsync(string connectionString)
    {
        int iteration = 0;
        int totalInserted = 0;

        while (iteration < MaxIterations)
        {
            iteration++;
          //  _logger.LogInformation("Sync iteration {Iteration} starting...", iteration);

            var messages = await CallReceiveAsync(start: -1, maxSize: BatchSize);

            if (messages == null || messages.Count == 0)
            {
                _logger.LogInformation("No messages returned from web service. Stopping.");
                break;
            }

            _logger.LogInformation("Received {Count} messages in iteration {Iteration}",
                messages.Count, iteration);

            // پارس همه‌ى پیام‌ها — بدون فیلتر تاریخ
            var newRows = new List<dynamic>();

            foreach (var msg in messages)
            {
                var timeStr = msg.Element("time")?.Value;
                var apiId = msg.Element("id")?.Value;
                var from = msg.Element("from")?.Value;
                var to = msg.Element("to")?.Value;
                var type = msg.Element("type")?.Value;
                var encoding = msg.Element("encoding")?.Value;
                var messageText = msg.Element("content")?.Value;
                var msgStatus = msg.Element("status")?.Value;

                if (string.IsNullOrEmpty(timeStr) || string.IsNullOrEmpty(apiId))
                    continue;

                if (!DateTime.TryParse(timeStr, null,
                    DateTimeStyles.RoundtripKind, out DateTime dt))
                    continue;

                var (sendDateShamsi, sendTimeShamsi) = ConvertToShamsi(dt);

                newRows.Add(new
                {
                    ApiId = apiId,
                    MobileSender = from,
                    Reciever = to,
                    SendDate = sendDateShamsi,
                    SendTime = sendTimeShamsi,
                    MessageKind = MapMessageKind(type),
                    MessageCoding = MapMessageCoding(encoding),
                    MessageText = messageText,
                    MessageStatus = MapMessageStatus(msgStatus)
                });
            }

            if (newRows.Count == 0)
            {
                _logger.LogInformation("No valid messages in this batch. Stopping.");
                break;
            }

            var inserted = await InsertNewRowsAsync(connectionString, newRows);
            totalInserted += inserted;

            _logger.LogInformation("Iteration {Iteration}: {Inserted} rows inserted.",
                iteration, inserted);

            if (inserted == 0)
            {
                _logger.LogInformation(
                    "No new rows inserted. All {Count} messages already in DB. Stopping.",
                    newRows.Count);
                break;
            }

            if (messages.Count < BatchSize)
            {
                _logger.LogInformation("Received less than batch size. Stopping.");
                break;
            }

            if (iteration < MaxIterations)
            {
                _logger.LogInformation("Waiting {Delay}ms before next call...", DelayBetweenCallsMs);
                await Task.Delay(DelayBetweenCallsMs);
            }
        }

        _logger.LogInformation(
            "Sync finished. Iterations: {Iterations}, Total inserted: {Total}",
            iteration, totalInserted);
    }

    // ═══════════════════════════════════════════════════════════════
    //  درج پیام‌هاى جدید — بر اساس ApiId (بدون فیلتر تاریخ)
    // ═══════════════════════════════════════════════════════════════
    private async Task<int> InsertNewRowsAsync(
        string connectionString,
        List<dynamic> newRows)
    {
        if (newRows.Count == 0) return 0;

        var apiIds = newRows
            .Select(r => (string)r.ApiId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (apiIds.Count == 0) return 0;

        // ۱. چک ApiIdهاى موجود
        HashSet<string> existingApiIds;

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            var existingList = await conn.QueryAsync<string>(
                $@"SELECT DISTINCT [ApiId] 
                   FROM {SmsReportTable} 
                   WHERE [ApiId] IN @ApiIds",
                new { ApiIds = apiIds });

            existingApiIds = new HashSet<string>(
                existingList.Where(x => !string.IsNullOrEmpty(x)));
        }

        // ۲. فقط ApiIdهاى جدید
        var rowsToInsert = newRows
            .Where(r => !existingApiIds.Contains((string)r.ApiId))
            .ToList();

        if (rowsToInsert.Count == 0)
            return 0;

        // ۳. اطلاعات مشتریان
        var senders = rowsToInsert
            .Select(r => (string)r.MobileSender)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(NormalizeMobile)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        var customersInfo = await GetCustomersInfoAsync(connectionString, senders);

        // ۴. درج
        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            foreach (var row in rowsToInsert)
            {
                var normalizedSender = NormalizeMobile((string)row.MobileSender);
                var info = customersInfo.ContainsKey(normalizedSender)
                    ? customersInfo[normalizedSender]
                    : null;

                await conn.ExecuteAsync($@"
                    INSERT INTO {SmsReportTable}
                        ([row],[ApiId],[MobileSender],[Reciever],[SendDate],[SendTime],
                         [MessageKind],[MessageCoding],[MessageText],[MessageStatus],
                         [CustomerName],[NationalCode],[ContractNo],
                         [LastPayDate],[Moavagh],[Tarh],[IsDeleted])
                    VALUES (0, @ApiId, @MobileSender, @Reciever, @SendDate, @SendTime,
                            @MessageKind, @MessageCoding, @MessageText, @MessageStatus,
                            @CustomerName, @NationalCode, @ContractNo,
                            @LastPayDate, @Moavagh, @Tarh, 0)",
                    new
                    {
                        row.ApiId,
                        row.MobileSender,
                        row.Reciever,
                        row.SendDate,
                        row.SendTime,
                        row.MessageKind,
                        row.MessageCoding,
                        row.MessageText,
                        row.MessageStatus,
                        CustomerName = info?.CustomerName,
                        NationalCode = info?.NationalCode,
                        ContractNo = info?.ContractNo,
                        LastPayDate = info?.LastPayDate,
                        Moavagh = info?.Moavagh,
                        Tarh = info?.Tarh
                    });
            }
        }

        return rowsToInsert.Count;
    }

    // ═══════════════════════════════════════════════════════════════
    //  فراخوانى SOAP API
    // ═══════════════════════════════════════════════════════════════
    private async Task<List<XElement>> CallReceiveAsync(long start, int maxSize)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        var soapBody = $@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
            <soap:Body>
                <receive xmlns=""http://tempuri.org/"">
                    <username>{SmsUserName}</username>
                    <password>{SmsPassword}</password>
                    <senderNumber>{SmsSenderNumber}</senderNumber>
                    <start>{start}</start>
                    <count>{maxSize}</count>
                </receive>
            </soap:Body>
        </soap:Envelope>";

        using var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "http://tempuri.org/receive");

        try
        {
            var response = await client.PostAsync(SmsServiceUrl, content);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            responseText = XmlHelper.CleanInvalidXmlChars(responseText);

            var xdoc = XDocument.Parse(responseText);
            xdoc = XmlHelper.RemoveNamespaces(xdoc);

            var receiveReturn = xdoc.Descendants("receiveReturn").FirstOrDefault();
            if (receiveReturn == null)
            {
                _logger.LogWarning("receiveReturn not found in SOAP response");
                return new List<XElement>();
            }

            var statusElem = receiveReturn.Element("status");
            if (statusElem == null || statusElem.Value != "0")
            {
                _logger.LogWarning("SMS web service returned error status: {Status}",
                    statusElem?.Value);
                return new List<XElement>();
            }

            var messagesContainer = receiveReturn.Element("messages");
            if (messagesContainer == null)
            {
                _logger.LogInformation("No messages container found in response");
                return new List<XElement>();
            }

            var messages = messagesContainer.Elements("messages").ToList();
            _logger.LogInformation("Parsed {Count} messages from SOAP response", messages.Count);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SMS receive web service");
            return new List<XElement>();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  اطلاعات مشتریان
    // ═══════════════════════════════════════════════════════════════
    private async Task<Dictionary<string, CustomerSmsInfo>> GetCustomersInfoAsync(
        string connectionString, List<string> cleanSenders)
    {
        var result = new Dictionary<string, CustomerSmsInfo>();
        if (cleanSenders.Count == 0) return result;

        const string query = @"
            SELECT
                '98' + SUBSTRING(Mobile1, 2, LEN(Mobile1)) AS CleanMobile,
                fname + ' ' + lname AS CustomerName,
                nationalCode AS NationalCode,
                cr.ContractNo AS ContractNo,
                TRY_CAST(g.LastpayDate AS INT) AS LastPayDate,
                TRY_CAST(g.Moavagh AS BIGINT) AS Moavagh,
                g.Tarh AS Tarh
            FROM leasing..funcustomer c WITH (NOLOCK)
            LEFT JOIN [Leasing].[dbo].[FunCustomerContacts] cn WITH (NOLOCK) 
                ON cn.cifref = c.serial
            LEFT JOIN [Leasing].[dbo].[FUNContract] cr WITH (NOLOCK) 
                ON cr.cifref = c.serial
            LEFT JOIN [DarikAssistant].[dbo].[ContractGrade] g WITH (NOLOCK) 
                ON g.ContractNo = cr.ContractNo
            WHERE '98' + SUBSTRING(Mobile1, 2, LEN(Mobile1)) IN @CleanMobiles
            ORDER BY g.LastPayDate DESC";

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var rows = await conn.QueryAsync<CustomerSmsInfo>(
            query, new { CleanMobiles = cleanSenders });

        foreach (var row in rows)
        {
            if (!string.IsNullOrEmpty(row.CleanMobile) && !result.ContainsKey(row.CleanMobile))
                result[row.CleanMobile] = row;
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    //  توابع کمکی
    // ═══════════════════════════════════════════════════════════════
    private static (int shamsiDate, string shamsiTime) ConvertToShamsi(DateTime dt)
    {
        // ═══════════════════════════════════════════════════════════
        //  تبدیل UTC → Iran Local (UTC+3:30)
        // ═══════════════════════════════════════════════════════════
        var utcDt = dt.Kind == DateTimeKind.Utc
            ? dt
            : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        TimeZoneInfo iranTz;
        try
        {
            iranTz = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        }
        catch
        {
            // Fallback: UTC+3:30 (بدون DST — ایران از ۲۰۲۲ DST نداره)
            var localDt = utcDt.AddHours(3).AddMinutes(30);
            var pcFallback = new PersianCalendar();
            int y = pcFallback.GetYear(localDt);
            int m = pcFallback.GetMonth(localDt);
            int d = pcFallback.GetDayOfMonth(localDt);
            return (y * 10000 + m * 100 + d, localDt.ToString("HH:mm"));
        }

        var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDt, iranTz);

        var pc = new PersianCalendar();
        int year = pc.GetYear(localDate);
        int month = pc.GetMonth(localDate);
        int day = pc.GetDayOfMonth(localDate);
        int shamsiDate = year * 10000 + month * 100 + day;
        string shamsiTime = localDate.ToString("HH:mm");

        return (shamsiDate, shamsiTime);
    }

    private static int ParseShamsiToInt(string shamsi)
    {
        return int.Parse(shamsi.Replace("-", "").Replace("/", ""));
    }

    private static string NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrEmpty(mobile)) return string.Empty;
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = "98" + digits.Substring(1);
        else if (!digits.StartsWith("98")) digits = "98" + digits;
        return digits;
    }

    private static string? NormalizeMobileForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // تبدیل ارقام فارسى/عربى به انگلیسى
        var persian = "۰۱۲۳۴۵۶۷۸۹";
        var arabic = "٠١٢٣٤٥٦٧٨٩";
        var sb = new StringBuilder();

        foreach (var c in input.Trim())
        {
            int pIndex = persian.IndexOf(c);
            int aIndex = arabic.IndexOf(c);

            if (pIndex >= 0) sb.Append((char)('0' + pIndex));
            else if (aIndex >= 0) sb.Append((char)('0' + aIndex));
            else sb.Append(c);
        }

        var cleaned = sb.ToString();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^\d+]", "");

        if (cleaned.StartsWith("+")) cleaned = cleaned.Substring(1);
        if (cleaned.StartsWith("0098")) cleaned = cleaned.Substring(4);
        else if (cleaned.StartsWith("98")) cleaned = cleaned.Substring(2);
        if (cleaned.StartsWith("0")) cleaned = cleaned.Substring(1);

        return cleaned;
    }

    private static string MapMessageKind(string? type) => type switch
    {
        "1" => "متن ساده",
        "2" => "فلش",
        "6" => "باینری",
        _ => type ?? string.Empty
    };

    private static string MapMessageCoding(string? encoding) => encoding switch
    {
        "1" => "لاتین",
        "2" => "یونیکد",
        _ => encoding ?? string.Empty
    };

    private static string MapMessageStatus(string? status) => status switch
    {
        "0" => "خوانده شده",
        "1" => "خوانده نشده",
        _ => status ?? string.Empty
    };

    // ═══════════════════════════════════════════════════════════════
    //  Update Customer Info
    // ═══════════════════════════════════════════════════════════════
    public async Task<UpdateCustomerInfoResult> UpdateCustomerInfoAsync(
        int id, string contractNo, long userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string userSql = @"
SELECT [DarikUserRef]
FROM [DarikAssistant].[app].[Users]
WHERE Id = @UserId;";

        var darikUserRef = await connection.QueryFirstOrDefaultAsync<int?>(
            userSql, new { UserId = userId });

        if (darikUserRef == null || darikUserRef <= 0)
        {
            return new UpdateCustomerInfoResult
            {
                IsSuccess = false,
                Message = "برای شما DarikUserRef ثبت نشده است. لطفاً با واحد IT تماس بگیرید."
            };
        }

        const string updateSql = @"
UPDATE S
SET 
    S.CustomerName               = cu.FName + ' ' + cu.LName,
    S.NationalCode               = cu.NationalCode,
    S.ContractNo                 = @ContractNo,
    S.LastPayDate                = TRY_CAST(g.LastpayDate AS INT),
    S.Moavagh                    = TRY_CAST(g.Moavagh AS BIGINT),
    S.Tarh                       = g.Tarh,
    S.UpdateCustomerInfoUserRef  = @UserRef,
    S.UpdateCustomerInfoDateTime = GETDATE()
FROM [DarikAssistant].[gen].[SmsReport] AS S
INNER JOIN [Leasing].[dbo].[FUNContract] AS C WITH (NOLOCK)
    ON C.[ContractNo] = @ContractNo
INNER JOIN [Leasing].[dbo].[FUNCustomer] AS cu WITH (NOLOCK)
    ON cu.Serial = C.CifRef
LEFT JOIN [DarikAssistant].[dbo].[ContractGrade] AS g WITH (NOLOCK)
    ON g.ContractNo = C.ContractNo
WHERE S.[Id] = @Id
  AND (S.[ContractNo] IS NULL OR S.[ContractNo] = '');";

        try
        {
            var affected = await connection.ExecuteAsync(updateSql, new
            {
                Id = id,
                ContractNo = contractNo,
                UserRef = darikUserRef.Value
            });

            if (affected <= 0)
            {
                return new UpdateCustomerInfoResult
                {
                    IsSuccess = false,
                    Message = "رکوردی برای به‌روزرسانی یافت نشد."
                };
            }

            return new UpdateCustomerInfoResult
            {
                IsSuccess = true,
                Message = "اطلاعات مشتری با موفقیت به‌روزرسانی شد.",
                AffectedRows = affected
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در به‌روزرسانی اطلاعات مشتری Id={Id}", id);
            return new UpdateCustomerInfoResult
            {
                IsSuccess = false,
                Message = "خطا در به‌روزرسانی دیتابیس."
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Set Alarm
    // ═══════════════════════════════════════════════════════════════
    public async Task<SetAlarmResult> SetAlarmAsync(
        int smsId, long userId, string answeredMessage, string contractNo)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string userSql = @"
SELECT [DarikUserRef]
FROM [DarikAssistant].[app].[Users]
WHERE Id = @UserId;";

        var darikUserRef = await connection.QueryFirstOrDefaultAsync<int?>(
            userSql, new { UserId = userId });

        if (darikUserRef == null || darikUserRef <= 0)
        {
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = "برای شما DarikUserRef ثبت نشده است. لطفاً با واحد IT تماس بگیرید."
            };
        }

        const string checkSql = @"
SELECT 
    S.[IsAnswered]   AS IsAnswered,
    S.[MobileSender] AS MobileSender,
    C.[Serial]       AS ContractSerial
FROM [DarikAssistant].[gen].[SmsReport] S WITH (NOLOCK)
LEFT JOIN [Leasing].[dbo].[FUNContract] C WITH (NOLOCK)
    ON C.ContractNo = S.ContractNo
WHERE S.[Id] = @Id;";

        var smsRow = await connection.QueryFirstOrDefaultAsync<dynamic>(
            checkSql, new { Id = smsId });

        if (smsRow == null)
        {
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = "رکورد پیامک یافت نشد."
            };
        }

        int? isAnswered = smsRow.IsAnswered;
        if (isAnswered == 1)
        {
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = "قبلاً پاسخ داده شده است."
            };
        }

        long? contractSerial = smsRow.ContractSerial;
        if (contractSerial == null || contractSerial <= 0)
        {
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = "این پیامک به هیچ قراردادی متصل نیست."
            };
        }

        string mobileSender = (string?)smsRow.MobileSender ?? string.Empty;

        const string updateSql = @"
UPDATE [DarikAssistant].[gen].[SmsReport]
SET 
    [IsAnswered]       = 1,
    [AnsweredUserRef]  = @AnsweredUserRef,
    [AnsweredMessage]  = @AnsweredMessage,
    [AnsweredDateTime] = GETDATE()
WHERE [Id] = @Id 
  AND ([IsAnswered] IS NULL OR [IsAnswered] = 0);";

        int affected;
        try
        {
            affected = await connection.ExecuteAsync(updateSql, new
            {
                Id = smsId,
                AnsweredUserRef = userId,
                AnsweredMessage = answeredMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در به‌روزرسانی SmsReport Id={Id}", smsId);
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = "خطا در به‌روزرسانی پیامک."
            };
        }

        if (affected <= 0)
        {
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = "به‌روزرسانی انجام نشد. احتمالاً این پیامک قبلاً پاسخ داده شده است."
            };
        }

        var alarmResult = await InsertAlarmInternalAsync(
            connection, userId, contractSerial.Value, answeredMessage,
            mobileSender, darikUserRef.Value);

        if (!alarmResult.IsSuccess)
        {
            return new SetAlarmResult
            {
                IsSuccess = false,
                Message = $"پیامک پاسخ داده شد ولی ثبت هشدار انجام نشد: {alarmResult.Message}",
                ContractSerial = contractSerial.Value,
                AffectedRows = affected
            };
        }

        return new SetAlarmResult
        {
            IsSuccess = true,
            Message = "پاسخ ثبت و هشدار با موفقیت ایجاد شد.",
            ContractSerial = contractSerial.Value,
            AffectedRows = affected
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  درج هشدار
    // ═══════════════════════════════════════════════════════════════
    private async Task<InsertAlarmResult> InsertAlarmInternalAsync(
        SqlConnection connection, long userId, long contractSerial,
        string message, string contactNumber, int darikUserRef)
    {
        message = CommonMethod.ConvertPersianToArabic(message) ?? string.Empty;
        const string insertSql = @"
INSERT INTO Leasing.dbo.FUNCuAlarm 
(
    CifRef, ContractRef, RegisterDate, AlarmDate, AlarmTime,
    AlarmStatus, AlarmDsr, AlarmType, SenderRef, Ranking,
    Cost, UsanceDate, ContactNumber, PursuerRef, FutureAct,
    CntSuspendAlarm
)
SELECT 
    NULL                                                AS CifRef,
    @ContractRef                                        AS ContractRef,
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
FROM (SELECT @ContractRef AS ContractRef) AS D
LEFT OUTER JOIN Leasing.dbo.GeneralPayment# AS GP WITH (NOLOCK)
    ON GP.ContractRef = D.ContractRef;";

        try
        {
            var affected = await connection.ExecuteAsync(insertSql, new
            {
                ContractRef = contractSerial,
                Message = message,
                DarikUserRef = darikUserRef,
                ContactNumber = contactNumber
            });

            if (affected <= 0)
            {
                return new InsertAlarmResult
                {
                    IsSuccess = false,
                    Message = "درج هشدار انجام نشد."
                };
            }

            return new InsertAlarmResult
            {
                IsSuccess = true,
                Message = "هشدار با موفقیت ثبت شد."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در درج هشدار برای قرارداد {ContractSerial}", contractSerial);
            return new InsertAlarmResult
            {
                IsSuccess = false,
                Message = "خطا در ثبت هشدار در دیتابیس."
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sync عمومى (براى Background Service و اندپوینت)
    // ═══════════════════════════════════════════════════════════════
    public async Task SyncAsync()
    {
        await SyncSmsFromWebServiceAsync(_connectionString);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Debug: دریافت خام از API
    // ═══════════════════════════════════════════════════════════════
    public async Task<DebugSmsResponse> DebugFetchRawAsync(int count = 250)
    {
        var result = new DebugSmsResponse();

        try
        {
            var messages = await CallReceiveAsync(start: -1, maxSize: count);

            if (messages == null || messages.Count == 0)
            {
                result.IsSuccess = false;
                result.Message = "هیچ پیامى از API دریافت نشد.";
                return result;
            }

            result.TotalFetched = messages.Count;

            var items = new List<DebugSmsItem>();
            int minDate = int.MaxValue;
            int maxDate = int.MinValue;

            foreach (var msg in messages)
            {
                var timeStr = msg.Element("time")?.Value;
                var id = msg.Element("id")?.Value;
                var from = msg.Element("from")?.Value;
                var to = msg.Element("to")?.Value;
                var content = msg.Element("content")?.Value;
                var type = msg.Element("type")?.Value;
                var encoding = msg.Element("encoding")?.Value;
                var status = msg.Element("status")?.Value;

                string? timeShamsi = null;

                if (!string.IsNullOrEmpty(timeStr))
                {
                    if (DateTime.TryParse(timeStr, null,
                        DateTimeStyles.RoundtripKind, out DateTime dt))
                    {
                        var (shamsiDate, shamsiTime) = ConvertToShamsi(dt);
                        timeShamsi = $"{shamsiDate:D8} {shamsiTime}";

                        if (shamsiDate < minDate) minDate = shamsiDate;
                        if (shamsiDate > maxDate) maxDate = shamsiDate;
                    }
                }

                items.Add(new DebugSmsItem
                {
                    Id = id,
                    From = from,
                    To = to,
                    Time = timeStr,
                    TimeShamsi = timeShamsi,
                    Content = content?.Length > 100 ? content.Substring(0, 100) + "..." : content,
                    Type = type,
                    Encoding = encoding,
                    Status = status
                });
            }

            result.IsSuccess = true;
            result.Message = $"{messages.Count} پیام دریافت شد.";
            result.MinShamsiDate = minDate == int.MaxValue ? null : minDate.ToString();
            result.MaxShamsiDate = maxDate == int.MinValue ? null : maxDate.ToString();
            result.Items = items;

            _logger.LogInformation(
                "DebugFetchRaw: {Count} messages, shamsi range {Min} to {Max}",
                items.Count,
                result.MinShamsiDate ?? "null",
                result.MaxShamsiDate ?? "null");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در DebugFetchRaw");
            result.IsSuccess = false;
            result.Message = $"خطا: {ex.Message}";
            return result;
        }
    }
}