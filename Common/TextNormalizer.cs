using System.Text;

namespace StaffPortal.Common;

public static class TextNormalizer
{
    /// <summary>
    /// نرمال‌سازى متن فارسى/عربى براى مقایسه و جستجو
    /// باید دقیقاً با DarikAssistant.dbo.fn_NormalizePersian هماهنگ باشد
    /// </summary>
    public static string? NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            // ─── ۱. حذف کاراکترهاى نامرئى ───
            if (c == '\u200B' ||  // ZWSP
                c == '\u200C' ||  // ZWNJ (نیم‌فاصله)
                c == '\u200D' ||  // ZWJ
                c == '\u200E' ||  // LRM
                c == '\u200F' ||  // RLM
                c == '\uFEFF' ||  // BOM
                c == '\u0640')    // کشیده (tatweel)
                continue;

            // ─── ۲. حذف اعراب (tashkeel) ───
            if (c >= '\u064B' && c <= '\u065F')
                continue;

            // ─── ۳. حذف جداکننده‌ها ───
            if (c == ' ' || c == '-' || c == '.' || c == '_' ||
                c == '\t' || c == '\n' || c == '\r' ||
                c == '\u060C' ||  // ، (ویرگول عربى)
                c == '\u061B' ||  // ؛ (نقطه‌ویرگول عربى)
                c == '\u061F')    // ؟ (علامت سؤال عربى)
                continue;

            // ─── ۴. تبدیل ارقام فارسى به انگلیسى ───
            if (c >= '\u06F0' && c <= '\u06F9')  // ۰ تا ۹
            {
                sb.Append((char)('0' + (c - '\u06F0')));
                continue;
            }

            // ─── ۵. تبدیل ارقام عربى به انگلیسى ───
            if (c >= '\u0660' && c <= '\u0669')  // ٠ تا ٩
            {
                sb.Append((char)('0' + (c - '\u0660')));
                continue;
            }

            // ─── ۶. نرمال‌سازى حروف ───
            char normalized = c switch
            {
                // ی/ى/ئ → ي
                '\u06CC' => '\u064A',  // ی → ي
                '\u0649' => '\u064A',  // ى → ي
                '\u0626' => '\u064A',  // ئ → ي

                // ک → ك
                '\u06A9' => '\u0643',  // ک → ك

                // آ/أ/إ/ٱ → ا
                '\u0622' => '\u0627',  // آ → ا
                '\u0623' => '\u0627',  // أ → ا
                '\u0625' => '\u0627',  // إ → ا
                '\u0671' => '\u0627',  // ٱ → ا

                // ة/ۀ/ۃ/ۂ → ه
                '\u0629' => '\u0647',  // ة → ه
                '\u06C0' => '\u0647',  // ۀ → ه
                '\u06C3' => '\u0647',  // ۃ → ه
                '\u06C2' => '\u0647',  // ۂ → ه

                // ؤ → و
                '\u0624' => '\u0648',  // ؤ → و

                _ => c
            };
            sb.Append(normalized);
        }

        var result = sb.ToString();
        if (result.Length == 0) return null;

        // ─── ۷. یکسان‌سازى «الله» با «اله» ───
        result = result.Replace("الله", "اله");

        return result;
    }
}