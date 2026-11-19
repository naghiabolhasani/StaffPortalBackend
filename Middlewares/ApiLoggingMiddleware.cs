using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Middlewares;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    private static readonly HashSet<string> _blockedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/swagger/index.html",
        "/favicon.ico",
        "/swagger/swagger-ui.css",
        "/swagger/swagger-ui-bundle.js",
        "/swagger/swagger-ui-standalone-preset.js",
        "/swagger/v1/swagger.json"
    };

    public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiLogService logService)
    {
        var path = context.Request.Path.ToString();

        // ─── مسیرهای مستثنی ───
        if (path.StartsWith("/Files/") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/_framework") ||
            path.StartsWith("/_content"))
        {
            await _next(context);
            return;
        }

        if (_blockedPaths.Contains(path))
        {
            await _next(context);
            return;
        }

        // ═══════════════════════════════════════════════════════════
        // ✅ استخراج UserId از هدر
        // ═══════════════════════════════════════════════════════════
        int? userId = null;
        if (context.Request.Headers.TryGetValue("UserId", out var userIdValues))
        {
            var userIdStr = userIdValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsed))
            {
                userId = parsed;
            }
        }

        // ─── خواندن Body درخواست ───
        context.Request.EnableBuffering();
        var inputJson = await ReadRequestBodyAsync(context);
        context.Request.Body.Position = 0;

        // ─── جایگزینی Response Body ───
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            var outputJson = await ReadResponseBodyAsync(context);
            var userIp = CommonMethod.GetClientIp(context);

            // ═══════════════════════════════════════════════════════
            // ✅ ثبت لاگ با UserId
            // ═══════════════════════════════════════════════════════
            await logService.EditLogAsync(
                apiName: path,
                inputJson: inputJson,
                outputJson: outputJson,
                userIp: userIp,
                userId: userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApiLoggingMiddleware");
            throw;
        }
        finally
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            if (body?.Length > 5000)
                body = body[..5000] + "... (truncated)";
            return string.IsNullOrEmpty(body) ? null : body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading request body");
            return null;
        }
    }

    private async Task<string?> ReadResponseBodyAsync(HttpContext context)
    {
        try
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            if (body?.Length > 8000)
                body = body[..8000] + "... (truncated)";
            return string.IsNullOrEmpty(body) ? null : body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading response body");
            return null;
        }
    }
}