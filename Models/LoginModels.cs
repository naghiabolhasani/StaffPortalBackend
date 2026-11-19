using System.Collections.Generic;

namespace StaffPortal.Models;

public class LoginModels
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Mobile { get; set; }
    public string? MaskedMobile { get; set; }
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// لیست نام مجوزها (مثل "Report.ProfitPerMonth")
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

public class StaffLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class StaffPermissionsRequest
{
    public string Username { get; set; } = string.Empty;
}


public class StaffPermissionsResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Mobile { get; set; }

    /// <summary>
    /// لیست کامل مجوزها با تمام جزئیات
    /// </summary>
    public List<UserPermissionDto> Permissions { get; set; } = new();

    /// <summary>
    /// فقط نام مجوزها به‌صورت رشته (مثل "Report.ProfitPerMonth")
    /// </summary>
    public List<string> PermissionNames { get; set; } = new();
}


