using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class ContractFacilityEndpoints
{
    public static void MapStaffContractFacilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ContractFacility")
            .WithTags("Contract Facility")
            .RequireAuthorization();

        // ============================================
        // ۱. جستجوی تسهیلات قرارداد
        // ============================================
        group.MapPost("/search", HandleSearch)
            .WithName("SearchContractFacility")
            .WithDescription("جستجوی اطلاعات تسهیلات قرارداد بر اساس شماره قرارداد")
            .Produces<StandardResponse<ContractFacilityDto>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(404)
            .Produces<StandardResponse<object>>(500);

        // ============================================
        // ۲. به‌روزرسانی شماره تسهیلات
        // ============================================
        group.MapPost("/UpdateFacilityId", HandleUpdateFacilityId)
            .WithName("UpdateContractFacilityId")
            .WithDescription("به‌روزرسانی شماره تسهیلات قرارداد")
            .Produces<StandardResponse<object>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(404)
            .Produces<StandardResponse<object>>(500);

        // ============================================
        // ۳. به‌روزرسانی تاریخ تصویب
        // ============================================
        group.MapPost("/UpdateRatificationDate", HandleUpdateRatificationDate)
            .WithName("UpdateContractRatificationDate")
            .WithDescription("به‌روزرسانی تاریخ تصویب قرارداد (فرمت شمسی: yyyymmdd)")
            .Produces<StandardResponse<object>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(404)
            .Produces<StandardResponse<object>>(500);
    }

    // ============================================================
    // هندلرها
    // ============================================================

    private static async Task<IResult> HandleSearch(
        [FromBody] ContractFacilitySearchRequest request,
        [FromServices] IContractFacilityService service,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ContractNo))
            {
                return Results.Json(
                    StandardResponse<object>.Failure("شماره قرارداد اجباری است."),
                    statusCode: 400);
            }

            var result = await service.SearchAsync(request.ContractNo);
            if (result == null)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("قراردادی با این شماره یافت نشد."),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<ContractFacilityDto>
            {
                IsSuccess = true,
                Data = result,
                Message = "اطلاعات تسهیلات قرارداد دریافت شد.",
                Count = 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching contract facility {ContractNo}", request.ContractNo);
            return Results.Json(
                StandardResponse<object>.Failure(ex.Message),
                statusCode: 400);
        }
    }

    private static async Task<IResult> HandleUpdateFacilityId(
     [FromBody] UpdateFacilityIdRequest request,
     [FromServices] IContractFacilityService service,
     [FromServices] ILogger<Program> logger)
    {
        try
        {
            // ═══════════════════════════════════════════════════════════
            //  اعتبارسنجی اولیه
            // ═══════════════════════════════════════════════════════════
            if (string.IsNullOrWhiteSpace(request.ContractNo))
            {
                return Results.Json(
                    StandardResponse<object>.Failure("شماره قرارداد اجباری است."),
                    statusCode: 400);
            }

            if (string.IsNullOrWhiteSpace(request.FacilityId))
            {
                return Results.Json(
                    StandardResponse<object>.Failure("شناسه تسهیلات اجباری است."),
                    statusCode: 400);
            }

            // ═══════════════════════════════════════════════════════════
            //  امنیت ContractNo (فقط رقم)
            // ═══════════════════════════════════════════════════════════
            if (!InputValidator.IsSafeString(request.ContractNo, @"^\d{1,30}$"))
            {
                logger.LogWarning(
                    "ورودی غیرمجاز در ContractNo: {Value}",
                    request.ContractNo);

                return Results.Json(
                    StandardResponse<object>.Failure("شماره قرارداد نامعتبر است."),
                    statusCode: 400);
            }

            // ═══════════════════════════════════════════════════════════
            //  امنیت FacilityId (حروف انگلیسی، ارقام، نقطه، آندرلاین)
            // ═══════════════════════════════════════════════════════════
            if (!InputValidator.IsSafeFacilityId(request.FacilityId))
            {
                logger.LogWarning(
                    "ورودی غیرمجاز در FacilityId: {Value}",
                    request.FacilityId);

                return Results.Json(
                    StandardResponse<object>.Failure(
                        "شناسه تسهیلات فقط می‌تواند شامل حروف انگلیسی، ارقام، نقطه و آندرلاین باشد."),
                    statusCode: 400);
            }

            // ═══════════════════════════════════════════════════════════
            //  فراخوانى سرویس
            // ═══════════════════════════════════════════════════════════
            await service.UpdateFacilityIdAsync(request.ContractNo, request.FacilityId);

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = "شناسه تسهیلات با موفقیت به‌روزرسانی شد.",
                Data = null,
                Count = 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating FacilityId for {ContractNo}", request.ContractNo);

            if (ex.Message.Contains("یافت نشد"))
            {
                return Results.Json(
                    StandardResponse<object>.Failure(ex.Message),
                    statusCode: 404);
            }

            return Results.Json(
                StandardResponse<object>.Failure(ex.Message),
                statusCode: 400);
        }
    }

    private static async Task<IResult> HandleUpdateRatificationDate(
    [FromBody] UpdateRatificationDateRequest request,
    [FromServices] IContractFacilityService service,
    [FromServices] ILogger<Program> logger)
    {
        try
        {
            // ═══════════════════════════════════════════════════════════
            //  اعتبارسنجى اولیه
            // ═══════════════════════════════════════════════════════════
            if (string.IsNullOrWhiteSpace(request.ContractNo))
            {
                return Results.Json(
                    StandardResponse<object>.Failure("شماره قرارداد اجباری است."),
                    statusCode: 400);
            }

            if (string.IsNullOrWhiteSpace(request.RatificationDate))
            {
                return Results.Json(
                    StandardResponse<object>.Failure("تاریخ انعقاد اجباری است."),
                    statusCode: 400);
            }

            // ═══════════════════════════════════════════════════════════
            //  امنیت ContractNo (فقط رقم)
            // ═══════════════════════════════════════════════════════════
            if (!InputValidator.IsSafeString(request.ContractNo, @"^\d{1,30}$"))
            {
                logger.LogWarning(
                    "ورودی غیرمجاز در ContractNo: {Value}",
                    request.ContractNo);

                return Results.Json(
                    StandardResponse<object>.Failure("شماره قرارداد نامعتبر است."),
                    statusCode: 400);
            }

            // ═══════════════════════════════════════════════════════════
            //  اعتبارسنجى تاریخ شمسی (yyyymmdd)
            // ═══════════════════════════════════════════════════════════
            if (!InputValidator.IsValidPersianDate(request.RatificationDate, out var dateError))
            {
                logger.LogWarning(
                    "ورودی غیرمجاز در RatificationDate: {Value} — {Error}",
                    request.RatificationDate, dateError);

                return Results.Json(
                    StandardResponse<object>.Failure(dateError),
                    statusCode: 400);
            }

            // ═══════════════════════════════════════════════════════════
            //  فراخوانى سرویس
            // ═══════════════════════════════════════════════════════════
            await service.UpdateRatificationDateAsync(
                request.ContractNo,
                request.RatificationDate);

            return Results.Ok(new StandardResponse<object>
            {
                IsSuccess = true,
                Message = "تاریخ انعقاد با موفقیت به‌روزرسانی شد.",
                Data = null,
                Count = 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating RatificationDate for {ContractNo}", request.ContractNo);

            if (ex.Message.Contains("یافت نشد"))
            {
                return Results.Json(
                    StandardResponse<object>.Failure(ex.Message),
                    statusCode: 404);
            }

            return Results.Json(
                StandardResponse<object>.Failure(ex.Message),
                statusCode: 400);
        }
    }
}