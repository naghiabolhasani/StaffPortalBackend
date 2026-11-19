using System.Text.RegularExpressions;

namespace StaffPortal.Common;

public static class InputValidator
{
    // ═══════════════════════════════════════════════════════════════
    //  FacilityId
    // ═══════════════════════════════════════════════════════════════
    private static readonly Regex SafeFacilityIdPattern = new Regex(
        @"^[A-Za-z0-9._]+$", RegexOptions.Compiled);

    public static bool IsSafeFacilityId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return SafeFacilityIdPattern.IsMatch(input);
    }

    // ═══════════════════════════════════════════════════════════════
    //  تاریخ شمسى yyyymmdd
    // ═══════════════════════════════════════════════════════════════
    private static readonly Regex PersianDatePattern = new Regex(
        @"^\d{8}$", RegexOptions.Compiled);

    /// <summary>
    /// بررسى اینکه رشته یک تاریخ شمسى معتبر به فرمت yyyymmdd باشد
    /// </summary>
    public static bool IsValidPersianDate(string? input, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "تاریخ نمی‌تواند خالی باشد.";
            return false;
        }

        if (!PersianDatePattern.IsMatch(input))
        {
            errorMessage = "تاریخ باید ۸ رقم و به فرمت yyyymmdd باشد (مثال: 14050101).";
            return false;
        }

        int year = int.Parse(input.Substring(0, 4));
        int month = int.Parse(input.Substring(4, 2));
        int day = int.Parse(input.Substring(6, 2));

        if (year < 1300 || year > 1500)
        {
            errorMessage = "سال باید بین ۱۳۰۰ تا ۱۵۰۰ باشد.";
            return false;
        }

        if (month < 1 || month > 12)
        {
            errorMessage = "ماه باید بین ۰۱ تا ۱۲ باشد.";
            return false;
        }

        int maxDay;
        if (month <= 6)
            maxDay = 31;
        else if (month <= 11)
            maxDay = 30;
        else
            maxDay = IsPersianLeapYear(year) ? 30 : 29;

        if (day < 1 || day > maxDay)
        {
            errorMessage = $"روز باید بین ۰۱ تا {maxDay:D2} باشد.";
            return false;
        }

        return true;
    }

    private static bool IsPersianLeapYear(int year)
    {
        int[] leapYears = { 1, 5, 9, 13, 17, 22, 26, 30 };
        return leapYears.Contains(year % 33);
    }

    // ═══════════════════════════════════════════════════════════════
    //  امنیت عمومى رشته
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// بررسى عمومى امنیت رشته بدون الگوى خاص
    /// </summary>
    public static bool IsSafeString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;
        var dangerous = new[] { '\'', '"', ';', '-', '/', '*', '\\', '(', ')', '=', '<', '>', '|', ' ' };
        return !input.Any(c => dangerous.Contains(c));
    }

    /// <summary>
    /// بررسى امنیت رشته با الگوى سفید (whitelist)
    /// </summary>
    public static bool IsSafeString(string? input, string allowedPattern)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;
        return Regex.IsMatch(input, allowedPattern);
    }

    // ═══════════════════════════════════════════════════════════════
    //  کاراکترهاى کنترلى
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// بررسى وجود کاراکتر کنترلى خطرناک در رشته
    /// (به‌جز tab, newline, carriage return)
    /// </summary>
    public static bool ContainsControlCharacters(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        foreach (var c in input)
        {
            if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                return true;
        }
        return false;
    }

    /// <summary>
    /// اعتبارسنجى کامل یک متن ورودى:
    /// - طول مجاز
    /// - نداشتن کاراکتر کنترلى
    /// </summary>
    public static bool IsSafeText(string? input, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        if (input.Length > maxLength) return false;

        if (ContainsControlCharacters(input)) return false;

        return true;
    }
}