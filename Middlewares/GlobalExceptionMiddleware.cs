using StaffPortal.Common;
using StaffPortal.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; // ← اضافه شد
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StaffPortal.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiLogService apiLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (fileName, methodName, lineNumber) = GetExceptionLocation(ex);
            var errorMessage = $"Unhandled exception in {methodName} ({fileName}:{lineNumber}) - {ex.Message}";
            _logger.LogError(ex, errorMessage);

            var inputJson = JsonSerializer.Serialize(new
            {
                Path = context.Request.Path,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.ToString()
            });

            var outputJson = JsonSerializer.Serialize(new
            {
                ExceptionType = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                FileName = fileName,
                MethodName = methodName,
                LineNumber = lineNumber
            });

            await apiLogService.EditLogAsync(
                apiName: "GlobalException",
                inputJson: inputJson,
                outputJson: outputJson,
                userIp: CommonMethod.GetClientIp(context)
            );

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var response = new { IsSuccess = false, Message = "خطای داخلی سرور. لطفاً با پشتیبانی تماس بگیرید." };
            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static (string FileName, string MethodName, int LineNumber) GetExceptionLocation(Exception ex)
    {
        var stackTrace = new StackTrace(ex, true);
        var frames = stackTrace.GetFrames();
        if (frames == null)
            return ("Unknown", "Unknown", 0);

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            var declaringType = method?.DeclaringType;
            if (declaringType != null && declaringType.Namespace != null &&
                declaringType.Namespace.StartsWith("StaffPortal"))
            {
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();
                var methodName = method?.Name ?? "Unknown";
                return (Path.GetFileName(fileName) ?? "Unknown", methodName, lineNumber);
            }
        }

        var lastFrame = frames.LastOrDefault();
        if (lastFrame != null)
        {
            var method = lastFrame.GetMethod();
            var fileName = lastFrame.GetFileName();
            var lineNumber = lastFrame.GetFileLineNumber();
            var methodName = method?.Name ?? "Unknown";
            return (Path.GetFileName(fileName) ?? "Unknown", methodName, lineNumber);
        }

        return ("Unknown", "Unknown", 0);
    }
}