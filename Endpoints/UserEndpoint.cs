using StaffPortal.Auth;
using StaffPortal.Data;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using StaffPortal.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaffPortal.Endpoints;

public static class UserEndpoint
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // ===== تنظیم رمز عبور کاربر =====
        app.MapPost("/user/set-password", SetPassword)
            .WithName("SetPassword")
            .WithTags("User")
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);

        // ===== لاگین کارکنان =====
        app.MapPost("/user/login", StaffLogin)
            .WithName("StaffLogin")
            .WithTags("User")
            .AllowAnonymous()
            .Produces<ApiResponse<LoginModels>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(500);

        // ===== دریافت مجوزهای کاربر =====
        app.MapPost("/user/permissions", GetStaffPermissions)
            .WithName("GetStaffPermissions")
            .WithTags("User")
            .RequireAuthorization()
            .Produces<ApiResponse<StaffPermissionsResponse>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<object>>(500);
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler: تنظیم رمز عبور
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> SetPassword(
        [FromBody] SetPasswordRequest request,
        [FromServices] IValidator<SetPasswordRequest> validator,
        [FromServices] AppDbContext dbContext,
        [FromServices] ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.Json(new ApiResponse<object>
            {
                IsSuccess = false,
                Message = errors,
                Data = null
            }, statusCode: 400);
        }

        try
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.Id);

            if (user == null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "کاربر یافت نشد",
                    Data = null
                }, statusCode: 404);
            }

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password));
            var hashedPassword = Convert.ToBase64String(hashedBytes);

            user.PasswordHash = hashedPassword;
            await dbContext.SaveChangesAsync();

            return Results.Ok(new ApiResponse<object>
            {
                IsSuccess = true,
                Message = "رمز عبور با موفقیت تنظیم شد",
                Data = null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting password for user {UserId}", request.Id);
            return Results.Json(new ApiResponse<object>
            {
                IsSuccess = false,
                Message = "خطای داخلی سرور",
                Data = null
            }, statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler: لاگین کارکنان
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> StaffLogin(
        [FromBody] StaffLoginRequest request,
        [FromServices] IValidator<StaffLoginRequest> validator,
        [FromServices] IStaffAuthService staffAuthService,
        [FromServices] ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.Json(new ApiResponse<object>
            {
                IsSuccess = false,
                Message = errors,
                Data = null
            }, statusCode: 400);
        }

        try
        {
            var (isSuccess, errorMessage, statusCode, data) =
                await staffAuthService.LoginAsync(request);

            if (!isSuccess || data == null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = errorMessage,
                    Data = null
                }, statusCode: statusCode);
            }

            return Results.Ok(new ApiResponse<LoginModels>
            {
                IsSuccess = true,
                Message = "ورود موفق",
                Data = data
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in staff login for {Username}", request.Username);
            return Results.Json(new ApiResponse<object>
            {
                IsSuccess = false,
                Message = "خطای داخلی سرور",
                Data = null
            }, statusCode: 500);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler: دریافت مجوزهای کاربر
    // ═══════════════════════════════════════════════════════════════
    private static async Task<IResult> GetStaffPermissions(
        [FromBody] StaffPermissionsRequest request,
        [FromServices] IValidator<StaffPermissionsRequest> validator,
        [FromServices] AppDbContext dbContext,
        [FromServices] IPermissionService permissionService,
        [FromServices] ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Results.Json(new ApiResponse<object>
            {
                IsSuccess = false,
                Message = errors,
                Data = null
            }, statusCode: 400);
        }

        try
        {
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Active == 1);

            if (user == null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "کاربری با این نام کاربری یافت نشد یا غیرفعال است",
                    Data = null
                }, statusCode: 404);
            }

            var permissions = await permissionService.GetUserPermissionsAsync(user.Id);

            var result = new StaffPermissionsResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Mobile = user.Mobile,
                Permissions = permissions,
                PermissionNames = permissions
                    .Where(p => !string.IsNullOrEmpty(p.Action))
                    .Select(p => $"{p.Resource}.{p.Action}")
                    .ToList()
            };

            return Results.Ok(new ApiResponse<StaffPermissionsResponse>
            {
                IsSuccess = true,
                Message = "مجوزها با موفقیت دریافت شدند",
                Data = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting permissions for username {Username}", request.Username);
            return Results.Json(new ApiResponse<object>
            {
                IsSuccess = false,
                Message = "خطای داخلی سرور",
                Data = null
            }, statusCode: 500);
        }
    }
}