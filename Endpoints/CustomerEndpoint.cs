using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StaffPortal.Auth;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class CustomerEndpoint
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════════════
        //  Search by Name
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/customer/search-by-name", HandleSearchByName)
            .WithName("CustomerSearchByName")
            .WithDescription("جستجوی پیشرفته مشتری (حداکثر ۱۰۰ نتیجه)")
            .WithTags("Customer")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.ComprehensiveCustomerProfiles")
            .Accepts<CustomerNameRequest>("application/json")
            .Produces<StandardResponse<List<CustomerModel>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);

        // 📌 اندپوینت‌هاى آینده (در صورت نیاز باز کن):
        // app.MapPost("/customer/get-by-national-code", HandleGetByNationalCode) ...
        // app.MapPost("/customer/search-by-tarh", HandleSearchByTarh) ...
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Search by Name
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleSearchByName(
        [FromBody] CustomerNameRequest request,
        [FromServices] ICustomerService service,
        [FromServices] ILogger<Program> logger)
    {
        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim().Length > 100)
        {
            return Results.Json(
                StandardResponse<object>.Failure("نام نباید بیش از ۱۰۰ کاراکتر باشد."),
                statusCode: 400);
        }

        var mobile = request.Mobile?.Trim();
        if (!string.IsNullOrWhiteSpace(mobile))
        {
            var normalized = StaffPortal.Common.TextNormalizer.NormalizeForSearch(mobile) ?? "";
            var digits = new string(normalized.Where(char.IsDigit).ToArray());
            if (digits.Length < 4 || digits.Length > 15)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("شماره موبایل باید بین ۴ تا ۱۵ رقم باشد."),
                    statusCode: 400);
            }
        }

        var nationalCode = request.NationalCode?.Trim();
        if (!string.IsNullOrWhiteSpace(nationalCode))
        {
            var digits = new string(nationalCode.Where(char.IsDigit).ToArray());
            if (digits.Length < 3 || digits.Length > 10)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("کد ملی باید بین ۳ تا ۱۰ رقم باشد."),
                    statusCode: 400);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ContractNo) && request.ContractNo.Trim().Length > 30)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره قرارداد نباید بیش از ۳۰ کاراکتر باشد."),
                statusCode: 400);
        }

        if (!string.IsNullOrWhiteSpace(request.InstallmentNo) && request.InstallmentNo.Trim().Length > 50)
        {
            return Results.Json(
                StandardResponse<object>.Failure("شماره تسهیلات نباید بیش از ۵۰ کاراکتر باشد."),
                statusCode: 400);
        }

        try
        {
            var data = await service.SearchByNameAsync(
                request.Name,
                mobile,
                nationalCode,
                request.ContractNo,
                request.InstallmentNo);

            return Results.Ok(new StandardResponse<List<CustomerModel>>
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
            logger.LogError(ex, "خطا در جستجوى مشتری با نام");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
}