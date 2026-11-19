using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Services;

public class PermissionService : IPermissionService
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;

    private const int CacheExpirationMinutes = 15;

    public PermissionService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    // ============================================================
    //  بررسی یک مجوز خاص
    // ============================================================
    public async Task<bool> HasPermissionAsync(int userId, string permissionName)
    {
        var cacheKey = $"UserPerms_{userId}";

        // بررسی کش
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPermissions)
            && cachedPermissions != null)
        {
            return cachedPermissions.Contains(permissionName);
        }

        // اگر کش نبود، همه مجوزها را بگیر و در کش بگذار
        var allPermissions = await GetUserPermissionsAsync(userId);
        var permissionSet = new HashSet<string>(
            allPermissions.Select(p => $"{p.Resource}.{p.Action}"),
            StringComparer.OrdinalIgnoreCase);

        _cache.Set(cacheKey, permissionSet, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return permissionSet.Contains(permissionName);
    }

    // ============================================================
    //  دریافت تمام مجوزهای کاربر
    // ============================================================
    public async Task<List<UserPermissionDto>> GetUserPermissionsAsync(int userId)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        var sql = @"
            WITH UserPermissions AS (
                -- ===== ۱. مجوزهای نقش‌های کاربر =====
                SELECT DISTINCT 
                    p.ID,
                    p.Resource,
                    p.Action,
                    p.ResourceFa,
                    p.ActionFa,
                    p.MenuTitle,
                    p.ParentRef,
                    p.SortIndex,
                    ur.PermissionCondition
                FROM [app].[UserRoles] ur
                INNER JOIN [app].[Roles] r ON r.Id = ur.RoleId
                INNER JOIN [app].[RolePermissions] rp ON rp.RoleId = ur.RoleId
                INNER JOIN [app].[Permissions] p ON p.ID = rp.PermissionId
                WHERE ur.UserId = @UserId
                  AND ur.RoleId IS NOT NULL
                  AND (ur.Active IS NULL OR ur.Active = 1)
                  AND (r.Active IS NULL OR r.Active = 1)         -- نقش فعال
                  AND (rp.Active IS NULL OR rp.Active = 1)        -- مجوز نقش فعال

                UNION

                -- ===== ۲. مجوزهای مستقیم کاربر =====
                SELECT 
                    p.ID,
                    p.Resource,
                    p.Action,
                    p.ResourceFa,
                    p.ActionFa,
                    p.MenuTitle,
                    p.ParentRef,
                    p.SortIndex,
                    ur.PermissionCondition
                FROM [app].[UserRoles] ur
                INNER JOIN [app].[Permissions] p ON p.ID = ur.PermissionRef
                WHERE ur.UserId = @UserId
                  AND ur.PermissionRef IS NOT NULL
                  AND (ur.Active IS NULL OR ur.Active = 1)
            )
            SELECT 
                up.Resource,
                up.Action,
                up.PermissionCondition AS Condition,
                up.ResourceFa,
                up.ActionFa,
                up.MenuTitle,
                up.ParentRef,
                up.SortIndex
            FROM UserPermissions up
            WHERE NOT EXISTS (
                -- ===== ۳. اعمال Override (غیرفعال‌سازی مستقیم) =====
                SELECT 1
                FROM [app].[UserRoles] ur2
                WHERE ur2.UserId = @UserId
                  AND ur2.PermissionRef = up.ID
                  AND ur2.RoleId IS NULL
                  AND ur2.Active = 0
            )
            ORDER BY up.SortIndex, up.Resource, up.Action;";

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var result = (await conn.QueryAsync<UserPermissionDto>(sql, new { UserId = userId }))
            .ToList();

        _logger.LogInformation("Loaded {Count} permissions for user {UserId}",
            result.Count, userId);

        return result;
    }

    // ============================================================
    //  پاک کردن کش
    // ============================================================
    public void InvalidateUserCache(int userId)
    {
        _cache.Remove($"UserPerms_{userId}");
    }
}