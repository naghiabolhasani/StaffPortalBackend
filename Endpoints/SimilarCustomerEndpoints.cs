using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StaffPortal.Auth;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class SimilarCustomerEndpoints
{
    public static void MapSimilarCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════════════
        //  Tarh List
        // ═══════════════════════════════════════════════════════════
        app.MapGet("/similar-customer/tarh", HandleGetTarhList)
            .WithName("SimilarCustomerTarhList")
            .WithDescription("دریافت لیست طرح‌ها")
            .WithTags("SimilarCustomer")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.SimilarCustomer")
            .Produces<StandardResponse<TarhListResponse>>(200)
            .Produces<StandardResponse<object>>(500);

        // ═══════════════════════════════════════════════════════════
        //  Search
        // ═══════════════════════════════════════════════════════════
        app.MapPost("/similar-customer/search", HandleSearch)
            .WithName("SimilarCustomerSearch")
            .WithDescription("جستجوی مشتریان بر اساس نام و طرح")
            .WithTags("SimilarCustomer")
            .RequireAuthorization()
            .RequirePermission("StaffMainPage.SimilarCustomer")
            .Accepts<SimilarCustomerSearchRequest>("application/json")
            .Produces<StandardResponse<List<SimilarCustomerSearchItem>>>(200)
            .Produces<StandardResponse<object>>(400)
            .Produces<StandardResponse<object>>(500);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Tarh List
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleGetTarhList(
        [FromServices] ISimilarCustomerService service,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            var data = await service.GetTarhListAsync();

            return Results.Ok(new StandardResponse<TarhListResponse>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Total
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت لیست طرح‌ها");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler: Search
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> HandleSearch(
        [FromBody] SimilarCustomerSearchRequest request,
        [FromServices] ISimilarCustomerService service,
        [FromServices] ILogger<Program> logger)
    {
        var name = request.Name?.Trim();
        var tarh = request.Tarh?.Trim();

        // حداکثر طول براى امنیت
        if (!string.IsNullOrEmpty(name) && name.Length > 100)
        {
            return Results.Json(
                StandardResponse<object>.Failure("نام مشتری نباید بیش از ۱۰۰ کاراکتر باشد."),
                statusCode: 400);
        }

        if (!string.IsNullOrEmpty(tarh) && tarh.Length > 200)
        {
            return Results.Json(
                StandardResponse<object>.Failure("نام طرح نباید بیش از ۲۰۰ کاراکتر باشد."),
                statusCode: 400);
        }

        try
        {
            var data = await service.SearchAsync(name, tarh);

            if (data == null || data.Count == 0)
            {
                return Results.Json(
                    StandardResponse<object>.Failure("داده‌ای یافت نشد"),
                    statusCode: 404);
            }

            return Results.Ok(new StandardResponse<List<SimilarCustomerSearchItem>>
            {
                IsSuccess = true,
                Message = "موفق",
                Data = data,
                Count = data.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در جستجوی مشتریان");
            return Results.Json(
                StandardResponse<object>.Failure("خطای داخلی سرور"),
                statusCode: 500);
        }
    }
}