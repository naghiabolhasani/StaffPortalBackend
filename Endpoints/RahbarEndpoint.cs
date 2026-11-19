using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StaffPortal.Auth;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class RahbarEndpoint
{
    public static void MapRahbarEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════════
        //  Inquiry
        // ═══════════════════════════════════════════════════════
        app.MapPost("/rahbar/inquiry", HandleInquiry)
            .WithName("RahbarInquiry")
            .WithDescription("استعلام اقساط از API بانک گلرنگ")
            .WithTags("Rahbar")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<RahbarInquiryRequest>("application/json")
            .Produces<StandardResponse<RahbarInquiryResult>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        app.MapPost("/rahbar/bulk-inquiry", HandleBulkInquiry)
    .WithName("RahbarBulkInquiry")
    .WithDescription("اجراى دستى استعلام گروهى (براى همه قراردادهاى Rahbar)")
    .WithTags("Rahbar")
    .RequireAuthorization()
    .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
    .Produces<StandardResponse<RahbarBulkInquiryResult>>(200)
    .Produces<StandardResponse<object>>(500);
    }
    private static async Task<IResult> HandleBulkInquiry(
    [FromServices] IRahbarService service,
    [FromServices] ILogger<Program> logger)
    {
        try
        {
            var result = await service.RunBulkInquiryAsync();

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در اجرا"),
                    statusCode: 500);
            }

            return Results.Ok(new StandardResponse<RahbarBulkInquiryResult>
            {
                IsSuccess = true,
                Message = result.Message ?? "استعلام گروهى انجام شد.",
                Data = result,
                Count = result.TotalProcessed
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در استعلام گروهى");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Handler
    // ═══════════════════════════════════════════════════════════
    private static async Task<IResult> HandleInquiry(
        [FromBody] RahbarInquiryRequest request,
        [FromServices] IRahbarService service,
        [FromServices] ILogger<Program> logger)
    {
        // ─── اعتبارسنجى NationalCode ───
        if (string.IsNullOrWhiteSpace(request.NationalCode))
        {
            return Results.Json(
                StandardResponse<object>.Failure("کد ملی اجباری است."),
                statusCode: 400);
        }

        if (!InputValidator.IsSafeString(request.NationalCode.Trim(), @"^\d{10}$"))
        {
            return Results.Json(
                StandardResponse<object>.Failure("کد ملی باید ۱۰ رقم باشد."),
                statusCode: 400);
        }

        // ─── اعتبارسنجى LoanReferenceCode ───
        if (string.IsNullOrWhiteSpace(request.LoanReferenceCode))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه تسهیلات اجباری است."),
                statusCode: 400);
        }

        if (request.LoanReferenceCode.Trim().Length > 50)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه تسهیلات نامعتبر است."),
                statusCode: 400);
        }

        try
        {
            var result = await service.InquiryAsync(request);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در استعلام"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<RahbarInquiryResult>
            {
                IsSuccess = true,
                Message = result.Message ?? "استعلام با موفقیت انجام شد.",
                Data = result,
                Count = result.TotalInstallments
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "خطا در Rahbar inquiry برای NC={NC}, FID={FID}",
                request.NationalCode, request.LoanReferenceCode);

            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
}