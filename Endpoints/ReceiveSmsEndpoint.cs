using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StaffPortal.Auth;
using StaffPortal.Common;
using StaffPortal.Data;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using StaffPortal.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class ReceiveSmsEndpoint
{
    public static void MapReceiveSmsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/recieve-sms/search", ReceiveSmsSearch)
            .WithName("ReceiveSms")
            .WithTags("Sms")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.InputSms")
            .Produces<StandardResponse<List<SmsReportItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(404)
            .Produces<StandardResponse<object>>(500);

   app.MapPost("/recieve-sms/UpdateCustomerInfo", UpdateCustomerInfo)
    .WithName("ReceiveSmsUpdateCustomerInfo")
    .WithDescription("به‌روزرسانی اطلاعات مشتری روی یک رکورد SmsReport")
    .WithTags("Sms")
    .RequireAuthorization()
    .RequirePermission("InputSms.UpdateCustomerInfo")
    .Accepts<UpdateCustomerInfoRequest>("application/json")
    .Produces<StandardResponse<object>>(200)
    .Produces<StandardResponse<object>>(400)
    .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  Set Alarm
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/recieve-sms/set-alarm", HandleSetAlarm)
            .WithName("ReceiveSmsSetAlarm")
            .WithDescription("ثبت پاسخ روى پیامک و ایجاد هشدار")
            .WithTags("Sms")
            .RequireAuthorization()
            .RequirePermission("InputSms.SetAlarm")
            .Accepts<SetAlarmRequest>("application/json")
            .Produces<StandardResponse<object>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);


        // ═══════════════════════════════════════════════════════════
        //  Debug: دریافت خام از API (بدون پردازش)
        // ═══════════════════════════════════════════════════════════
        //app.MapPost("/recieve-sms/debug-raw", HandleDebugRaw)
        //    .WithName("ReceiveSmsDebugRaw")
        //    .WithDescription("دریافت خام پیام‌ک‌ها از API (براى دیباگ)")
        //    .WithTags("Sms")
        //    .RequireAuthorization()
        //    .RequirePermission("StaffMainPage.InputSms")
        //    .Produces<StandardResponse<DebugSmsResponse>>(200)
        //    .Produces<StandardResponse<object>>(500);


    }

    private static async Task<IResult> HandleDebugRaw(
    [FromServices] IStaffSmsService staffSmsService,
    [FromServices] ILogger<Program> logger)
    {
        try
        {
            var data = await staffSmsService.DebugFetchRawAsync(250);

            return Results.Ok(new StandardResponse<DebugSmsResponse>
            {
                IsSuccess = data.IsSuccess,
                Message = data.Message ?? "پایان",
                Data = data,
                Count = data.TotalFetched
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در DebugRaw");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    private static async Task<IResult> HandleSetAlarm(
        HttpContext httpContext,
        [FromBody] SetAlarmRequest request,
        [FromServices] IStaffSmsService staffSmsService,
        [FromServices] ILogger<Program> logger)
    {
        // ═══════════════════════════════════════════════════════════
        //  خواندن UserId از هدر
        // ═══════════════════════════════════════════════════════════
        if (!httpContext.Request.Headers.TryGetValue("UserId", out var userIdHeader) ||
            !long.TryParse(userIdHeader.FirstOrDefault(), out long userId) ||
            userId <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("هدر UserId نامعتبر یا ناموجود است."),
                statusCode: 400);
        }

        // ═══════════════════════════════════════════════════════════
        //  اعتبارسنجی
        // ═══════════════════════════════════════════════════════════
        if (request.Id <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه پیامک (Id) معتبر نیست."),
                statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(request.ContractNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد اجباری است."),
                statusCode: 400);
        }

        var contractNo = request.ContractNo.Trim();

        if (!InputValidator.IsSafeString(contractNo, @"^\d{1,30}$"))
        {
            logger.LogWarning("ورودی غیرمجاز در ContractNo: {Value}", contractNo);
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد نامعتبر است."),
                statusCode: 400);
        }

        var answeredMessage = string.IsNullOrWhiteSpace(request.AnsweredMessage)
            ? "پاسخ داده شد"
            : request.AnsweredMessage.Trim();

        if (answeredMessage.Length > 500)
        {
            return Results.Json(
                StandardResponse<object>.Failure("متن پاسخ نباید بیش از ۵۰۰ کاراکتر باشد."),
                statusCode: 400);
        }

        if (!InputValidator.IsSafeText(answeredMessage, 500))
        {
            return Results.Json(
                StandardResponse<object>.Failure("متن پاسخ شامل کاراکترهای غیرمجاز است."),
                statusCode: 400);
        }

        // ═══════════════════════════════════════════════════════════
        //  فراخوانى سرویس
        // ═══════════════════════════════════════════════════════════
        try
        {
            var result = await staffSmsService.SetAlarmAsync(
                request.Id,
                userId,
                answeredMessage,
                contractNo);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در ثبت پاسخ"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = result.Message ?? "پاسخ ثبت و هشدار با موفقیت ایجاد شد.",
                Data = new { contractSerial = result.ContractSerial },
                Count = result.AffectedRows
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در SetAlarm برای Id={Id}", request.Id);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
    private static async Task<IResult> UpdateCustomerInfo(
    HttpContext httpContext,
    [FromBody] UpdateCustomerInfoRequest request,
    [FromServices] IStaffSmsService staffSmsService,
    [FromServices] ILogger<Program> logger)
    {
        // ═══════════════════════════════════════════════════════════
        //  خواندن UserId از هدر
        // ═══════════════════════════════════════════════════════════
        if (!httpContext.Request.Headers.TryGetValue("UserId", out var userIdHeader) ||
            !long.TryParse(userIdHeader.FirstOrDefault(), out long userId) ||
            userId <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("هدر UserId نامعتبر یا ناموجود است."),
                statusCode: 400);
        }

        // ═══════════════════════════════════════════════════════════
        //  اعتبارسنجی ورودی‌ها
        // ═══════════════════════════════════════════════════════════
        if (request.Id <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه رکورد (Id) معتبر نیست."),
                statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(request.ContractNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد اجباری است."),
                statusCode: 400);
        }

        var contractNo = request.ContractNo.Trim();

        if (!InputValidator.IsSafeString(contractNo, @"^\d{1,30}$"))
        {
            logger.LogWarning("ورودی غیرمجاز در ContractNo: {Value}", contractNo);
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد نامعتبر است."),
                statusCode: 400);
        }

        try
        {
            var result = await staffSmsService.UpdateCustomerInfoAsync(
                request.Id,
                contractNo,
                userId);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در به‌روزرسانی"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = result.Message ?? "اطلاعات مشتری با موفقیت به‌روزرسانی شد.",
                Data = null,
                Count = result.AffectedRows
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در به‌روزرسانی اطلاعات مشتری برای Id={Id}", request.Id);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }


    private static async Task<IResult> ReceiveSmsSearch(
    [FromBody] ReceiveSmsRequest request,
    [FromServices] IStaffSmsService staffSmsService,
    [FromServices] ILogger<Program> logger)
    {
        // ═══════════════════════════════════════════════════════════
        //  اعتبارسنجی تاریخ‌ها (اختیارى)
        // ═══════════════════════════════════════════════════════════
        if (!string.IsNullOrWhiteSpace(request.FromDate) &&
            !InputValidator.IsValidPersianDate(request.FromDate, out var err1))
        {
            return Results.Json(
                StandardResponse<object>.Failure($"تاریخ شروع: {err1}"),
                statusCode: 400);
        }

        if (!string.IsNullOrWhiteSpace(request.ToDate) &&
            !InputValidator.IsValidPersianDate(request.ToDate, out var err2))
        {
            return Results.Json(
                StandardResponse<object>.Failure($"تاریخ پایان: {err2}"),
                statusCode: 400);
        }

        // ═══════════════════════════════════════════════════════════
        //  اعتبارسنجی Mobile
        // ═══════════════════════════════════════════════════════════
        if (!string.IsNullOrWhiteSpace(request.Mobile) &&
            !InputValidator.IsSafeString(request.Mobile.Trim(), @"^0?\d{1,20}$"))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره موبایل نامعتبر است."),
                statusCode: 400);
        }

        try
        {
            var result = await staffSmsService.ReceiveSmsSearchAsync(request);

            if (result == null || result.Items.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            var pagination = new PaginationInfo
            {
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Results.Ok(StandardResponse<List<SmsReportItem>>.SuccessPaged(
                result.Items,
                pagination,
                "داده‌ها با موفقیت دریافت شدند"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ReceiveSmsSearch");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    //////////
}