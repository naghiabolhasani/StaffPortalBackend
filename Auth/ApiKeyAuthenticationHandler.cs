using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StaffPortal.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-KEY";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. بررسی X-API-KEY
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("هدر X-API-KEY یافت نشد."));
        }

        var providedApiKey = apiKeyValues.FirstOrDefault();
        var validApiKey = _configuration["ApiKey"];

        if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != validApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("ApiKey نامعتبر است."));
        }

        // 2. خواندن UserId از هدر (اگر وجود داشت)
        string userId = "1"; // مقدار پیش‌فرض
        if (Request.Headers.TryGetValue("UserId", out var userIdValues))
        {
            var userIdValue = userIdValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(userIdValue) && int.TryParse(userIdValue, out int parsedUserId))
            {
                userId = parsedUserId.ToString();
            }
        }

        // 3. ساخت Claims با UserId
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiClient"),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
            new Claim(ClaimTypes.NameIdentifier, userId) // ← اضافه شد
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";

        var response = new
        {
            IsSuccess = false,
            Message = "ApiKey نامعتبر است. لطفاً X-API-KEY معتبر ارسال کنید.",
            StatusCode = 401
        };

        await Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}