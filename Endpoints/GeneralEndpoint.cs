using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class GeneralEndpoint
{
    public static void MapGeneralEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════════
        //  Template Messages
        // ═══════════════════════════════════════════════════════
        app.MapPost("/general/template-message", HandleTemplateMessage)
     .WithName("GeneralTemplateMessage")
     .WithDescription("دریافت پیام‌های قالب بر اساس Kind (با فیلتر اختیاری Message)")
     .WithTags("General")
     .RequireAuthorization()
     .Accepts<GeneralTemplateMessageRequest>("application/json")
     .Produces<StandardResponse<List<GeneralTemplateMessageResponse>>>(200)   // ← تغییر
     .Produces<StandardResponse<object>>(400)
     .Produces<StandardResponse<object>>(500);
    }

    // ═══════════════════════════════════════════════════════════
    //  Handler
    // ═══════════════════════════════════════════════════════════
    private static async Task<IResult> HandleTemplateMessage(
        [FromBody] GeneralTemplateMessageRequest request,
        [FromServices] IGeneralService service,
        [FromServices] ILogger<Program> logger)
    {
        // اعتبارسنجى Kind
        if (request.Kind <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه Kind معتبر نیست."),
                statusCode: 400);
        }

        // اعتبارسنجى Message (اگر ارسال شده)
        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            if (request.Message.Length > 200)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("متن جستجو نباید بیش از ۲۰۰ کاراکتر باشد."),
                    statusCode: 400);
            }

            if (!InputValidator.IsSafeText(request.Message, 200))
            {
                return Results.Json(
                    StandardResponse<object>.Failure("متن جستجو شامل کاراکترهای غیرمجاز است."),
                    statusCode: 400);
            }
        }

        try
        {
            var data = await service.GetTemplateMessagesAsync(request.Kind, request.Message);

            return Results.Ok(new StandardResponse<List<GeneralTemplateMessageResponse>>   // ← تغییر
            {
                IsSuccess = true,
                Message = data.Count > 0
                    ? "داده‌ها با موفقیت دریافت شدند"
                    : "نتیجه‌ای یافت نشد",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "خطا در دریافت پیام‌هاى قالب برای Kind={Kind}, Message={Message}",
                request.Kind, request.Message);

            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
}