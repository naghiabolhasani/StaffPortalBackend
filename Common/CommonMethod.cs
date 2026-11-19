using Serilog.Core;
using System.Globalization;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.IO; // ← اضافه شد
using System.Linq;
using Microsoft.AspNetCore.Hosting;

using System.Text;
using Microsoft.EntityFrameworkCore;
using StaffPortal.Models;

namespace StaffPortal.Common;


public static class CommonMethod
{

    public static IResult ResponseUnauthorized(string message = "لطفاً مجدد وارد حساب کاربری خود شوید؛ توکن دسترسی شما منقضی شده است.")
    {
        return Results.Json(new { IsSuccess = false, Message = message }, statusCode: StatusCodes.Status401Unauthorized);
    }


    public static readonly HashSet<string> UserTokenEndpoints = new(StringComparer.OrdinalIgnoreCase)
{
       
        "/api/requestChange_cancelBySeller",
        "/api/requestChange_sellerProcess",
        "/api/request_betaSaleInquiry/",
    "/api/contract/insert",
    "/api/cash/insert"
};

    public static readonly HashSet<string> CustomerTokenEndpoints = new(StringComparer.OrdinalIgnoreCase)
{
   // "/api/contract/insert",
    "/api/request/new"
};

    public static string NormalizeMobileForSms(string mobile)
    {
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00"))
            digits = digits[2..];
        if (digits.StartsWith("0"))
            digits = "98" + digits[1..];
        else if (!digits.StartsWith("98"))
            digits = "98" + digits;
        if (digits.Length > 12)
            digits = digits[^12..];
        return digits;
    }


    /// <summary>
    /// تبدیل حروف فارسى به عربى براى ذخیره در دیتابیس
    /// (ی → ي، ک → ك، ۀ → ه، آ/أ/إ → ا، ة → ه، ؤ → و)
    /// </summary>
    /// <summary>
    /// تبدیل حروف فارسى (ی، ک) به عربى (ي، ك)
    /// </summary>
    public static string? ConvertPersianToArabic(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return input
            .Replace('ی', 'ي')   // ی فارسی → ي عربى
            .Replace('ک', 'ك');  // ک فارسی → ك عربى
    }

    /// <summary>
    /// برگرداندن تاریخ و ساعت شمسی جاری به فرمت فشرده yyyyMMddHHmmss (برای استفاده در نام فایل)
    /// </summary>
    public static string GetCurrentPersianTimestamp()
    {
        var pc = new PersianCalendar();
        var now = DateTime.Now;
        return $"{pc.GetYear(now):0000}" +
               $"{pc.GetMonth(now):00}" +
               $"{pc.GetDayOfMonth(now):00}" +
               $"{pc.GetHour(now):00}" +
               $"{pc.GetMinute(now):00}" +
               $"{pc.GetSecond(now):00}";
    }

    /// <summary>
    /// نرمالایز کردن شماره موبایل به فرمت استاندارد 11 رقمی با پیشوند 09
    /// </summary>
    /// <param name="rawMobile">شماره موبایل به هر فرمتی (مثل 0098912..., 0912..., +98912..., 912...)</param>
    /// <returns>شماره موبایل نرمالایز شده 11 رقمی با 09- یا null در صورت نامعتبر بودن</returns>
    public static string NormalizeMobileNumber(string? rawMobile)
    {
        if (string.IsNullOrWhiteSpace(rawMobile))
            return "";

        // 1. حذف همه کاراکترهای غیر عددی (فقط ارقام 0-9 باقی می‌مانند)
        var digits = new string(rawMobile.Where(char.IsDigit).ToArray());

        // 2. اگر تعداد ارقام کمتر از 10 باشد، نامعتبر
        if (digits.Length < 10)
            return "";

        // 3. گرفتن 10 رقم آخر (برای حذف پیشوندهای اضافی مانند 0098، 98، +98 و ...)
        var last10Digits = digits[^10..];

        // 4. بررسی اینکه آیا 10 رقم آخر با '9' شروع می‌شوند (شماره موبایل ایرانی)
        if (!last10Digits.StartsWith('9'))
            return "";

        // 5. نرمالایز نهایی: "0" + last10Digits
        return "0" + last10Digits;
    }

    public static string ConvertArabicToPersian(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // تبدیل اعداد فارسی/عربی به انگلیسی
        var numberMapping = new Dictionary<char, char>
        {
            {'۰', '0'}, {'۱', '1'}, {'۲', '2'}, {'۳', '3'}, {'۴', '4'},
            {'۵', '5'}, {'۶', '6'}, {'۷', '7'}, {'۸', '8'}, {'۹', '9'},
            {'٠', '0'}, {'١', '1'}, {'٢', '2'}, {'٣', '3'}, {'٤', '4'},
            {'٥', '5'}, {'٦', '6'}, {'٧', '7'}, {'٨', '8'}, {'٩', '9'}
        };

        // تبدیل حروف عربی به فارسی (حفظ آ فارسی)
        var letterMapping = new Dictionary<char, char>
        {
            {'ك', 'ک'},  // ک عربی → ک فارسی
            {'ي', 'ی'},  // ی عربی → ی فارسی
            {'ى', 'ی'},  // ی عربی دندانه‌دار → ی فارسی
            {'إ', 'ا'},  // ا با همزه پایین → ا
            {'أ', 'ا'},  // ا با همزه بالا → ا
            {'ة', 'ه'}   // ه گرد → ه
            // 'آ' را تبدیل نمی‌کنیم چون در فارسی صحیح است
        };

        var result = new StringBuilder();
        foreach (var c in input)
        {
            if (numberMapping.ContainsKey(c))
                result.Append(numberMapping[c]);
            else if (letterMapping.ContainsKey(c))
                result.Append(letterMapping[c]);
            else
                result.Append(c);
        }
        return result.ToString();
    }

    public static DateTime? ConvertPersianDateToGregorian(string persianDate)
{
    if (string.IsNullOrWhiteSpace(persianDate)) return null;
    var parts = persianDate.Split('/');
    if (parts.Length != 3) return null;
    if (!int.TryParse(parts[0], out int year) ||
        !int.TryParse(parts[1], out int month) ||
        !int.TryParse(parts[2], out int day))
        return null;
    var pc = new PersianCalendar();
    try
    {
        return pc.ToDateTime(year, month, day, 0, 0, 0, 0);
    }
    catch (Exception ex)
        {
            Console.WriteLine("TryCatchError:Method: ConvertPersianDateToGregorian " + ex.ToString());
            return null;
    }
}


    /// <summary>
    /// در محیط Development، شماره نسخه را یک واحد افزایش داده و در فایل ذخیره می‌کند.
    /// </summary>
    /// <summary>
    /// در محیط Development، شماره نسخه را یک واحد افزایش داده و در فایل ذخیره می‌کند.
    /// فایل هم در ریشه پروژه (قابل مشاهده در VS) و هم در bin آپدیت می‌شود.
    /// </summary>
    public static void IncrementVersionIfDevelopment(IWebHostEnvironment env)
    {
        if (!env.IsDevelopment()) return;

        // ═══════════════════════════════════════════════════════════
        //  دو مسیر: پروژه (منبع اصلى) و bin (براى ReadSettingVersion)
        // ═══════════════════════════════════════════════════════════
        string rootFilePath = Path.Combine(env.ContentRootPath, "CustomSetting.json");
        string binFilePath = Path.Combine(AppContext.BaseDirectory, "CustomSetting.json");

        try
        {
            // ═══════════════════════════════════════════════════════════
            //  خواندن از فایل پروژه (منبع اصلى)
            // ═══════════════════════════════════════════════════════════
            int currentVersion = 1;

            if (File.Exists(rootFilePath))
            {
                string json = File.ReadAllText(rootFilePath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("VersionNumber", out JsonElement element))
                    currentVersion = element.GetInt32();
            }

            int newVersion = currentVersion + 1;

            // ═══════════════════════════════════════════════════════════
            //  نوشتن در هر دو مسیر (هم محتوا)
            // ═══════════════════════════════════════════════════════════
            var updated = new { VersionNumber = newVersion };
            string newJson = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(rootFilePath, newJson);
            File.WriteAllText(binFilePath, newJson);

            Console.WriteLine($"✅ Version incremented to {newVersion}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطا در بروزرسانی CustomSetting.json: {ex.Message}");
        }
    }


    /// <summary>
    /// خواندن شماره نسخه از فایل CustomSetting.json، 
    /// 2️⃣ در وی‌ژوال استودیو، Pre-build event را تنظیم کنید (افزایش شماره در هر بیلد)
    //روی پروژه کلیک راست → Properties → Build Events.

    //در کادر Pre-build event command line دستور زیر را قرار دهید(PowerShell):

    //powershell
    //powershell -Command "$file = '$(ProjectDir)CustomSetting.json'; if (Test-Path $file) { $json = Get-Content $file -Raw | ConvertFrom-Json; $json.VersionNumber++; $json | ConvertTo-Json | Set-Content $file } else { @{ VersionNumber = 1 } | ConvertTo-Json | Set-Content $file }"
    /// </summary>
    public static int ReadSettingVersion()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "CustomSetting.json");
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("VersionNumber", out JsonElement element))
                    return element.GetInt32();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطا  در خواندن CustomSetting.json: {ex.Message}");
        }
        return 1; // مقدار پیش‌فرض
    }

    /// <summary>
    /// برگرداندن تاریخ شمسی جاری به فرمت yyyy/MM/dd
    /// </summary>
    public static string GetCurrentPersianDate()
    {
        var persianCalendar = new PersianCalendar();
        var now = DateTime.Now;
        return $"{persianCalendar.GetYear(now):0000}/{persianCalendar.GetMonth(now):00}/{persianCalendar.GetDayOfMonth(now):00}";
    }

    /// <summary>
    /// برگرداندن تاریخ و ساعت شمسی جاری به فرمت yyyy/MM/dd HH:mm:ss
    /// </summary>
    public static string GetCurrentPersianDateTime()
    {
        var persianCalendar = new PersianCalendar();
        var now = DateTime.Now;
        string persianDate = $"{persianCalendar.GetYear(now):0000}/{persianCalendar.GetMonth(now):00}/{persianCalendar.GetDayOfMonth(now):00}";
        string time = now.ToString("HH:mm:ss");
        return $"{persianDate} {time}";
    }

    /// <summary>
    /// دریافت IP واقعی کلاینت از HttpContext
    /// </summary>
    /// <param name="httpContext">HttpContext جاری</param>
    /// <returns>IP کلاینت</returns>
    public static string GetClientIp(HttpContext httpContext)
    {
        // اولویت 1: X-Forwarded-For (برای پروکسی و لودبالانسر)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        // اولویت 2: X-Real-IP (برای Nginx و برخی پروکسی‌ها)
        var realIp = httpContext.Request.Headers["X-Real-IP"].ToString();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        // اولویت 3: CF-Connecting-IP (برای Cloudflare)
        var cfIp = httpContext.Request.Headers["CF-Connecting-IP"].ToString();
        if (!string.IsNullOrEmpty(cfIp))
        {
            return cfIp;
        }
        
        // اولویت 4: IP مستقیم اتصال
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        
        // تبدیل IPv6 loopback به فرمت خوانا
        if (remoteIp == "::1" || remoteIp == "127.0.0.1")
        {
            return "localhost";
        }
        
        return remoteIp ?? "Unknown";
    }
    
    /// <summary>
    /// دریافت User-Agent از هدر درخواست
    /// </summary>
    /// <param name="httpContext">HttpContext جاری</param>
    /// <returns>User-Agent کلاینت</returns>
    public static string GetUserAgent(HttpContext httpContext)
    {
        return httpContext.Request.Headers["User-Agent"].ToString();
    }
    
    /// <summary>
    /// دریافت یک هدر خاص از درخواست
    /// </summary>
    /// <param name="httpContext">HttpContext جاری</param>
    /// <param name="headerName">نام هدر</param>
    /// <returns>مقدار هدر</returns>
    public static string GetHeader(HttpContext httpContext, string headerName)
    {
        return httpContext.Request.Headers[headerName].ToString();
    }
    
    /// <summary>
    /// دریافت تمام هدرهای درخواست (برای دیباگ)
    /// </summary>
    /// <param name="httpContext">HttpContext جاری</param>
    /// <returns>دیکشنری هدرها</returns>
    public static Dictionary<string, string> GetAllHeaders(HttpContext httpContext)
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in httpContext.Request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }
        return headers;
    }
}