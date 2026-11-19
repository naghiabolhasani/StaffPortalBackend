using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace StaffPortal.Middlewares;

public class DevelopmentAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;

    // ===== EnableFakeHeaders به‌صورت هارد کد =====
    private const bool EnableFakeHeaders = true; // برای فعال‌سازی مقدار true، برای غیرفعال‌سازی false
    private const string FakeHeaderUserId = "1"; // برای فعال‌سازی مقدار true، برای غیرفعال‌سازی false

    public DevelopmentAuthenticationMiddleware(
        RequestDelegate next,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        _next = next;
        _env = env;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ============================================================
        // فقط در محیط Development اجرا می‌شود
        // ============================================================
        if (_env.IsDevelopment() && EnableFakeHeaders)
        {
            // ===== ApiKey واقعی را از appsettings.json بخوان =====
            var realApiKey = _configuration["ApiKey"];

            // ===== FakeUserId را هارد کد کن =====
            var fakeUserId = FakeHeaderUserId; // یا هر عدد دلخواه

            // ===== 1. تنظیم هدر X-API-KEY (با مقدار واقعی) =====
            if (!string.IsNullOrEmpty(realApiKey))
            {
                context.Request.Headers["X-API-KEY"] = realApiKey;
            }

            // ===== 2. تنظیم هدر X-UserId (با مقدار فیک) =====
            context.Request.Headers["UserId"] = fakeUserId;

            // ===== 3. تنظیم Claims (برای سناریوهایی که هدر خوانده نمی‌شود) =====
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "DevelopmentUser"),
                new Claim(ClaimTypes.AuthenticationMethod, "Development"),
                new Claim(ClaimTypes.NameIdentifier, fakeUserId)
            };

            if (!string.IsNullOrEmpty(realApiKey))
                claims.Add(new Claim("ApiKey", realApiKey));

            var identity = new ClaimsIdentity(claims, "Development");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}