using System.Collections.Generic;
using System.Threading.Tasks;
using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IPermissionService
{
    /// <summary>
    /// بررسی یک مجوز خاص برای کاربر
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionName);

    /// <summary>
    /// دریافت تمام مجوزهای کاربر (برای ارسال به فرانت)
    /// </summary>
    Task<List<UserPermissionDto>> GetUserPermissionsAsync(int userId);

    /// <summary>
    /// پاک کردن کش مجوزهای یک کاربر
    /// </summary>
    void InvalidateUserCache(int userId);
}