using System.Text;
using System.Text.Json;

namespace SuperAppKamanAPI.Middlewares;

public class PersianNormalizationMiddleware
{
    private readonly RequestDelegate _next;

    public PersianNormalizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // فقط برای درخواست‌های POST, PUT, PATCH
        if (context.Request.Method == HttpMethods.Post ||
            context.Request.Method == HttpMethods.Put ||
            context.Request.Method == HttpMethods.Patch)
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrEmpty(body))
            {
                var convertedBody = NormalizeJson(body);
                
                if (!string.IsNullOrEmpty(convertedBody) && convertedBody != body)
                {
                    var byteArray = Encoding.UTF8.GetBytes(convertedBody);
                    context.Request.Body = new MemoryStream(byteArray);
                    context.Request.ContentLength = byteArray.Length;
                }
            }
        }

        await _next(context);
    }

    private string NormalizeJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var converted = ConvertElement(doc.RootElement);
            return JsonSerializer.Serialize(converted, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private object ConvertElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:           // ← ✅ این case اضافه شد
                return null;

            case JsonValueKind.String:
                var str = element.GetString();
                return NormalizePersianText(str ?? string.Empty);
            
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertElement(prop.Value);
                }
                return dict;
            
            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertElement(item));
                }
                return list;
            
            default:
                return element.GetRawText();
        }
    }

    private string NormalizePersianText(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
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
}