using FluentValidation;
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

public static class ComprehensiveCustomerProfileEndpoints
{
    public static void MapComprehensiveCustomerProfileEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════════════════
        //  Base Info (Query 1)
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/base-info", HandleBaseInfo)
            .WithName("ComprehensiveBaseInfo")
            .WithDescription("دریافت اطلاعات پایه مشتری (کد ملی یا شماره قرارداد)")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ComprehensiveBaseInfoRequest>("application/json")
            .Produces<StandardResponse<ComprehensiveBaseInfoItem>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        app.MapPost("/comprehensive-customer-profile/settlement-detail", HandleSettlementDetail)
    .WithName("ComprehensiveSettlementDetail")
    .WithDescription("جزئیات تسویه قرارداد (جریمه، تسویه، کارمزد وصول)")
    .WithTags("ComprehensiveCustomerProfile")
    .RequireAuthorization()
    .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
    .Accepts<ComprehensivePayDetailRequest>("application/json")
    .Produces<StandardResponse<List<SettlementDetailItem>>>(200)
    .Produces<StandardResponse<object>>(400)
    .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════
        //  Contract Detail
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/contract-detail", HandleContractDetail)
            .WithName("ComprehensiveContractDetail")
            .WithDescription("جزئیات قرارداد (اصل تسهیلات، فرع، نرخ، مدت، تعداد اقساط)")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<ContractDetailItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(404)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  Pay Detail (Query 2)
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/pay-detail", HandlePayDetail)
            .WithName("ComprehensivePayDetail")
            .WithDescription("دریافت جزئیات پرداخت مشتری (معوقات، اقساط، جریمه و تسویه)")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ComprehensivePayDetailRequest>("application/json")
            .Produces<StandardResponse<ComprehensivePayDetailItem>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════
        //  Update Rahbar
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/update-rahbar", HandleUpdateRahbar)
            .WithName("ComprehensiveUpdateRahbar")
            .WithDescription("استعلام از بانک و به‌روزرسانی Rahbar_Arrears بر اساس شماره قرارداد و تسهیلات")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<UpdateRahbarRequest>("application/json")
            .Produces<StandardResponse<UpdateRahbarResult>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
        // ═══════════════════════════════════════════════════════════════
        //  SendSms
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/send-sms", HandleSendSms)
            .WithName("ComprehensiveSendSms")
            .WithDescription("پیامک‌های ارسالی به مشتری")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
             .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<SendSmsItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  ReceiveSms
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/recieve-sms", HandleReceiveSms)
            .WithName("ComprehensiveReceiveSms")
            .WithDescription("پیامک‌های دریافتی از مشتری")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<ReceiveSmsItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  Deposit
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/deposit", HandleDeposit)
            .WithName("ComprehensiveDeposit")
            .WithDescription("واریزی‌ها، چک‌ها و استردادهای مشتری")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<DepositItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  FunPayment
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/funpayment", HandleFunPayment)
            .WithName("ComprehensiveFunPayment")
            .WithDescription("لیست اقساط قرارداد")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
                .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<FunPaymentItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  Sponser
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/sponser", HandleSponser)
            .WithName("ComprehensiveSponser")
            .WithDescription("تامین‌کننده مالى قرارداد")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
               .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<SponserRequest>("application/json")
            .Produces<StandardResponse<List<SponserItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
        // ═══════════════════════════════════════════════════════════════
        //  Treasury
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/treasury", HandleTreasury)
            .WithName("ComprehensiveTreasury")
            .WithDescription("اطلاعات خزانه قرارداد (چک‌های در جریان)")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
               .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<TreasuryItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
        // ═══════════════════════════════════════════════════════════════
        //  Checks
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/checks", HandleChecks)
            .WithName("ComprehensiveChecks")
            .WithDescription("لیست چک‌های قرارداد (دریافتی و پرداختی)")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<ContractRefRequest>("application/json")
            .Produces<StandardResponse<List<ChecksItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
        // ═══════════════════════════════════════════════════════════════
        //  Alarms
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/alarms", HandleAlarms)
            .WithName("ComprehensiveAlarms")
            .WithDescription("لیست هشدارهای قرارداد")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<AlarmsRequest>("application/json")
            .Produces<StandardResponse<List<AlarmsItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
        // ═══════════════════════════════════════════════════════════════
        //  Insert Alarm
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/insert-alarm", HandleInsertAlarm)
            .WithName("ComprehensiveInsertAlarm")
            .WithDescription("ثبت هشدار جدید برای قرارداد")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("ComprehensiveCustomerProfiles.InsertAlarm")
            .Accepts<InsertAlarmRequest>("application/json")
            .Produces<StandardResponse<object>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════════
        //  Update Contacts
        // ═══════════════════════════════════════════════════════════════
        app.MapPost("/comprehensive-customer-profile/update-contacts", HandleUpdateContacts)
            .WithName("ComprehensiveUpdateContacts")
            .WithDescription("به‌روزرسانی شماره تماس مشتری (Mobile1 / Mobile2)")
            .WithTags("ComprehensiveCustomerProfile")
            .RequireAuthorization()
            .RequirePermission("ComprehensiveCustomerProfiles.UpdateMobile")
            .Accepts<UpdateContactsRequest>("application/json")
            .Produces<StandardResponse<object>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
        // ═══════════════════════════════════════════════════════════════
        //  
        // ═══════════════════════════════════════════════════════════════

        app.MapPost("/comprehensive-customer-profile/send-template-sms", HandleSendTemplateSms)
    .WithName("ComprehensiveSendTemplateSms")
    .WithDescription("جایگزینی placeholderهای قالب پیام با اطلاعات قرارداد")
    .WithTags("ComprehensiveCustomerProfile")
    .RequireAuthorization()
    .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
    .Accepts<SendTemplateSmsRequest>("application/json")
    .Produces<StandardResponse<SendTemplateSmsResult>>(200)
    .Produces<StandardResponse<object>>(400)
    .Produces<StandardResponse<object>>(500);
    }


    private static async Task<IResult> HandleSendTemplateSms(
        HttpContext httpContext,
        [FromBody] SendTemplateSmsRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
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
        //  اعتبارسنجى
        // ═══════════════════════════════════════════════════════════
        if (request.TemplateMessageId <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قالب پیام (TemplateMessageId) معتبر نیست."),
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
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد نامعتبر است."),
                statusCode: 400);
        }

        try
        {
            // ⚠️ اینجا باید ۳ پارامتر باشه (userId)
            var result = await service.SendTemplateSmsAsync(
                request.TemplateMessageId,
                contractNo,
                userId);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در پردازش"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<SendTemplateSmsResult>
            {
                IsSuccess = true,
                Message = result.Message ?? "پیام با موفقیت ثبت شد.",
                Data = result,
                Count = 1
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "خطا در SendTemplateSms برای TemplateMessageId={Id}, ContractNo={ContractNo}",
                request.TemplateMessageId, contractNo);

            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
    private static async Task<IResult> HandleContractDetail(
    [FromBody] ContractRefRequest request,
    [FromServices] IComprehensiveCustomerProfileService service,
    [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetContractDetailAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<ContractDetailItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت جزئیات قرارداد برای ContractRef={ContractRef}", request.ContractRef);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    private static async Task<IResult> HandleUpdateRahbar(
    [FromBody] UpdateRahbarRequest request,
    [FromServices] IComprehensiveCustomerProfileService service,
    [FromServices] ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.ContractNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد اجباری است."),
                statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(request.InstallmentNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه تسهیلات اجباری است."),
                statusCode: 400);
        }

        var contractNo = request.ContractNo.Trim();
        var installmentNo = request.InstallmentNo.Trim();

        if (!InputValidator.IsSafeString(contractNo, @"^\d{1,30}$"))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد نامعتبر است."),
                statusCode: 400);
        }

        if (installmentNo.Length > 50)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه تسهیلات نامعتبر است."),
                statusCode: 400);
        }

        try
        {
            var result = await service.UpdateRahbarAsync(contractNo, installmentNo);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در استعلام"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<UpdateRahbarResult>
            {
                IsSuccess = true,
                Message = result.Message ?? "استعلام با موفقیت انجام شد.",
                Data = result,
                Count = result.ArrearsInstallmentCount
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در UpdateRahbar برای {ContractNo}", contractNo);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
    private static async Task<IResult> HandleSettlementDetail(
    [FromBody] ComprehensivePayDetailRequest request,
    [FromServices] IComprehensiveCustomerProfileService service,
    [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetSettlementDetailAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<SettlementDetailItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت جزئیات تسویه");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    private static async Task<IResult> HandleUpdateContacts(
    [FromBody] UpdateContactsRequest request,
    [FromServices] IComprehensiveCustomerProfileService service,
    [FromServices] ILogger<Program> logger)
    {
        // ═══════════════════════════════════════════════════════════
        //  اعتبارسنجی ContractNo
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        //  اعتبارسنجی Mobile1 و Mobile2
        //  - دقیقاً ۱۱ رقم
        //  - فقط عدد
        //  - شروع با صفر
        // ═══════════════════════════════════════════════════════════
        string? mobile1 = null;
        string? mobile2 = null;

        if (!string.IsNullOrWhiteSpace(request.Mobile1))
        {
            mobile1 = request.Mobile1.Trim();

            if (!InputValidator.IsSafeString(mobile1, @"^0\d{10}$"))
            {
                logger.LogWarning("ورودی غیرمجاز در Mobile1: {Value}", mobile1);
                return Results.Json(
                    StandardResponse<object>.Failure(
                        "شماره موبایل ۱ باید ۱۱ رقم، فقط عددی و با صفر شروع شود (مثال: 09121234567)."),
                    statusCode: 400);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Mobile2))
        {
            mobile2 = request.Mobile2.Trim();

            if (!InputValidator.IsSafeString(mobile2, @"^0\d{10}$"))
            {
                logger.LogWarning("ورودی غیرمجاز در Mobile2: {Value}", mobile2);
                return Results.Json(
                    StandardResponse<object>.Failure(
                        "شماره موبایل ۲ باید ۱۱ رقم، فقط عددی و با صفر شروع شود (مثال: 09121234567)."),
                    statusCode: 400);
            }
        }

        if (mobile1 == null && mobile2 == null)
        {
            return Results.Json(
                StandardResponse<object>.Failure(
                    "حداقل یکی از شماره‌ها (Mobile1 یا Mobile2) باید ارسال شود."),
                statusCode: 400);
        }

        // ═══════════════════════════════════════════════════════════
        //  فراخوانی سرویس
        // ═══════════════════════════════════════════════════════════
        try
        {
            var result = await service.UpdateContactsAsync(contractNo, mobile1, mobile2);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در به‌روزرسانی"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = result.Message ?? "شماره تماس با موفقیت به‌روزرسانی شد.",
                Data = null,
                Count = result.AffectedRows
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در به‌روزرسانی شماره تماس برای قرارداد {ContractNo}", contractNo);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Checks
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleChecks(
        [FromBody] ContractRefRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetChecksAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<ChecksItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت چک‌ها");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Treasury
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleTreasury(
        [FromBody] ContractRefRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetTreasuryAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<TreasuryItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت اطلاعات خزانه");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Sponser
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleSponser(
        [FromBody] SponserRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        var contractNo = request.ContractNo?.Trim();

        if (string.IsNullOrEmpty(contractNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد الزامی است."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetSponserAsync(contractNo);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای برای تامین کننده مالی یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<SponserItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت اطلاعات تامین کننده مالی");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: SendSms
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleSendSms(
        [FromBody] ContractRefRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetSendSmsAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<SendSmsItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت پیامک‌های ارسالی");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: ReceiveSms
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleReceiveSms(
        [FromBody] ContractRefRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetReceiveSmsAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<ReceiveSmsItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت پیامک‌های دریافتی");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Deposit
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleDeposit(
        [FromBody] ContractRefRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetDepositAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<DepositItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت واریزی‌ها");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: FunPayment
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleFunPayment(
        [FromBody] ContractRefRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetFunPaymentAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<FunPaymentItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت اقساط");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler: Base Info
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleBaseInfo(
        [FromBody] ComprehensiveBaseInfoRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        var nationalCode = request.NationalCode?.Trim();
        var contractNo = request.ContractNo?.Trim();

        if (string.IsNullOrEmpty(nationalCode) && string.IsNullOrEmpty(contractNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure(
                    "حداقل یکی از فیلدها (کد ملی یا شماره قرارداد) باید وارد شود."),
                statusCode: 400);
        }

        if (!string.IsNullOrEmpty(nationalCode) &&
            (nationalCode.Length != 10 || !nationalCode.All(char.IsDigit)))
        {
            return Results.Json(
                StandardResponse<object>.Failure("کد ملی باید ۱۰ رقم باشد."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetBaseInfoAsync(nationalCode, contractNo);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<ComprehensiveBaseInfoItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت اطلاعات پایه ComprehensiveCustomerProfile");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler: Pay Detail
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandlePayDetail(
      [FromBody] ComprehensivePayDetailRequest request,
      [FromServices] IComprehensiveCustomerProfileService service,
      [FromServices] ILogger<Program> logger)
    {
        if (request.ContractRef <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractRef) معتبر نیست."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetPayDetailAsync(request.ContractRef);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<ComprehensivePayDetailItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت جزئیات پرداخت ComprehensiveCustomerProfile");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
    ///////////
    ///// ═══════════════════════════════════════════════════════════════
    //  Handler: Alarms
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleAlarms(
        [FromBody] AlarmsRequest request,
        [FromServices] IComprehensiveCustomerProfileService service,
        [FromServices] ILogger<Program> logger)
    {
        var contractNo = request.ContractNo?.Trim();

        if (string.IsNullOrEmpty(contractNo))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد الزامی است."),
                statusCode: 400);
        }

        try
        {
            var data = await service.GetAlarmsAsync(contractNo);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<AlarmsItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت هشدارها");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Handler: Alarms
        // ═══════════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════════
        //  Handler: Alarms
        // ═══════════════════════════════════════════════════════════════
    }


    private static async Task<IResult> HandleInsertAlarm(
    HttpContext httpContext,
    [FromBody] InsertAlarmRequest request,
    [FromServices] IComprehensiveCustomerProfileService service,
    [FromServices] ILogger<Program> logger)
    {
        // ═══════════════════════════════════════════════════════════
        //  ۱. خواندن UserId از هدر
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
        //  ۲. اعتبارسنجی ورودی‌ها
        // ═══════════════════════════════════════════════════════════
        if (request.ContractSerial <= 0)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شناسه قرارداد (ContractSerial) معتبر نیست."),
                statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.Json(
                StandardResponse<object>.Failure("متن پیام اجباری است."),
                statusCode: 400);
        }

        if (request.Message.Length > 500)
        {
            return Results.Json(
                StandardResponse<object>.Failure("متن پیام نباید بیش از ۵۰۰ کاراکتر باشد."),
                statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(request.ContactNumber))
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره تماس اجباری است."),
                statusCode: 400);
        }

        var trimmedContact = request.ContactNumber.Trim();

        if (!InputValidator.IsSafeString(trimmedContact, @"^[0-9+\-\s()]{1,30}$"))
        {
            logger.LogWarning(
                "ورودی غیرمجاز در ContactNumber: {Value}",
                trimmedContact);

            return Results.Json(
                StandardResponse<object>.Failure(
                    "شماره تماس فقط می‌تواند شامل ارقام، فاصله، +، - و پرانتز باشد."),
                statusCode: 400);
        }

        var trimmedMessage = request.Message.Trim();

        if (!InputValidator.IsSafeText(trimmedMessage, maxLength: 500))
        {
            logger.LogWarning(
                "ورودی غیرمجاز در Message: {Value}",
                trimmedMessage);

            return Results.Json(
                StandardResponse<object>.Failure(
                    "متن پیام نامعتبر است یا بیش از حد طولانی است."),
                statusCode: 400);
        }

        // ═══════════════════════════════════════════════════════════
        //  ۳. فراخوانی سرویس
        // ═══════════════════════════════════════════════════════════
        try
        {
            var result = await service.InsertAlarmAsync(
                userId,
                request.ContractSerial,
                trimmedMessage,
                trimmedContact);

            if (!result.IsSuccess)
            {
                return Results.Json(
                    StandardResponse<object>.Failure(result.Message ?? "خطا در ثبت هشدار"),
                    statusCode: 400);
            }

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = result.Message ?? "هشدار با موفقیت ثبت شد.",
                Data = null,
                Count = 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در ثبت هشدار برای قرارداد {ContractSerial}", request.ContractSerial);
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }


    
}