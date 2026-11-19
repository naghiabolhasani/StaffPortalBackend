using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Services;

public class StaffAuthService : IStaffAuthService
{
    private const string RequiredPermission = "StaffMainPage.Access";

    private readonly Data.AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<StaffAuthService> _logger;

    public StaffAuthService(
        Data.AppDbContext dbContext,
        IPermissionService permissionService,
        ILogger<StaffAuthService> logger)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<(bool IsSuccess, string ErrorMessage, int StatusCode, LoginModels? Data)> LoginAsync(
    StaffLoginRequest request)
    {
        // 1. جستجوی کاربر فعال
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Active == 1);

        if (user == null)
        {
            return (false, "نام کاربری یا رمز عبور اشتباه است", 401, null);
        }

        // 2. بررسی رمز عبور
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return (false, "نام کاربری یا رمز عبور اشتباه است", 401, null);
        }

        // 3. بررسی مجوز StaffMainPage.Access
        var hasAccess = await _permissionService.HasPermissionAsync(user.Id, RequiredPermission);

        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} ({Username}) attempted staff login without permission",
                user.Id, user.Username);

            return (false, "شما دسترسی به پنل کارکنان را ندارید", 403, null);
        }

        // 4. دریافت نام مجوزهای کاربر
        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);

        // 5. ساخت خروجی — فقط نام مجوزها
        var result = new LoginModels
        {
            UserId = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Mobile = user.Mobile,
            MaskedMobile = MaskMobileNumber(user.Mobile ?? string.Empty),
            MustChangePassword = string.IsNullOrEmpty(user.PasswordHash),
            Permissions = permissions
                .Where(p => !string.IsNullOrEmpty(p.Action))
                .Select(p => $"{p.Resource}.{p.Action}")
                .ToList()
        };

        _logger.LogInformation("Staff login successful for user {UserId} ({Username})",
            user.Id, user.Username);

        return (true, "ورود موفق", 200, result);
    }
    // ============================================================
    //  توابع کمکی
    // ============================================================
    private static bool VerifyPassword(string password, string? passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
            return false;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hashedPassword = Convert.ToBase64String(hashedBytes);

        return hashedPassword == passwordHash;
    }

    private static string MaskMobileNumber(string mobile)
    {
        if (string.IsNullOrEmpty(mobile) || mobile.Length < 11)
            return mobile;

        var firstPart = mobile.Substring(0, 4);
        var lastPart = mobile.Substring(8, 3);
        var masked = $"{firstPart}****{lastPart}";

        return ToPersianDigits(masked);
    }

    private static string ToPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var persianDigits = new Dictionary<char, char>
        {
            {'0', '۰'}, {'1', '۱'}, {'2', '۲'}, {'3', '۳'}, {'4', '۴'},
            {'5', '۵'}, {'6', '۶'}, {'7', '۷'}, {'8', '۸'}, {'9', '۹'}
        };

        return new string(input.Select(c => persianDigits.TryGetValue(c, out var p) ? p : c).ToArray());
    }
}