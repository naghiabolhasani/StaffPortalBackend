using System;
using System.Linq;
using System.Threading.Tasks;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Endpoints;

public static class DynamicReportEndpoint
{
    public static void MapDynamicReportEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════════════
        //  API 1: لیست گزارش‌ها
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/dynamic-report/list", GetReportList)
            .WithName("DynamicReportList")
            .WithTags("DynamicReport")
            .RequireAuthorization()
            .Produces<StandardResponse<object>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════
        //  API 2: اطلاعات گزارش
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/dynamic-report/report-info", GetReportInfo)
            .WithName("DynamicReportInfo")
            .WithTags("DynamicReport")
            .RequireAuthorization()
            .Produces<StandardResponse<DynamicReportInfoResponse>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(404)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════
        //  API 3: اجرای گزارش
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/dynamic-report/run", RunReport)
            .WithName("DynamicReportRun")
            .WithTags("DynamicReport")
            .RequireAuthorization()
            .Produces<StandardResponse<DynamicReportRunResponse>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
    }

    // ═══════════════════════════════════════════════════════════
    //  Handler 1: لیست گزارش‌ها
    // ═══════════════════════════════════════════════════════════
    private static async Task<IResult> GetReportList(
        [FromBody] DynamicReportListRequest request,
        [FromServices] IDynamicReportService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.UserId <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("UserId معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var categories = await service.GetReportsForUserAsync(request.UserId);

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = "لیست گزارش‌ها با موفقیت دریافت شد",
                Data = categories,
                Count = 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting report list for user {UserId}", request.UserId);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Handler 2: اطلاعات گزارش
    // ═══════════════════════════════════════════════════════════
    private static async Task<IResult> GetReportInfo(
        [FromBody] DynamicReportInfoRequest request,
        [FromServices] IDynamicReportService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ReportId <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("ReportId معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var info = await service.GetReportInfoAsync(request.ReportId);

            return Results.Ok(new StandardResponse<DynamicReportInfoResponse>
            {
                IsSuccess = true,
                Message = "اطلاعات گزارش با موفقیت دریافت شد",
                Data = info,
                Count = 0
            });
        }
        catch (ArgumentException ex)
        {
            return Results.Json(
                StandardResponse<object>.Failure(ex.Message),
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting report info {ReportId}", request.ReportId);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Handler 3: اجرای گزارش
    // ═══════════════════════════════════════════════════════════
    private static async Task<IResult> RunReport(
        HttpContext context,
        [FromBody] DynamicReportRunRequest request,
        [FromServices] IDynamicReportService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ReportId <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("ReportId معتبر نیست."),
                statusCode: 400);
        }

        // دریافت UserId از هدر
        if (!context.Request.Headers.TryGetValue("UserId", out var userIdValues))
        {
            return Results.Json(
                StandardResponse<object>.Failure("هدر UserId یافت نشد."),
                statusCode: 401);
        }

        if (!int.TryParse(userIdValues.FirstOrDefault(), out int userId))
        {
            return Results.Json(
                StandardResponse<object>.Failure("فرمت UserId نامعتبر است."),
                statusCode: 401);
        }

        try
        {
            var result = await service.RunReportAsync(request, userId);

            return Results.Ok(new StandardResponse<DynamicReportRunResponse>
            {
                IsSuccess = true,
                Message = "گزارش با موفقیت اجرا شد",
                Data = result,
                Count = 0
            });
        }
        catch (ArgumentException ex)
        {
            return Results.Json(
                StandardResponse<object>.Failure(ex.Message),
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running report {ReportId}", request.ReportId);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
}