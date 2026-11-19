using System.Security.Claims;
using System.Threading.Tasks;
using StaffPortal.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace StaffPortal.Auth;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly IWebHostEnvironment _env;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        IWebHostEnvironment env)
    {
        _permissionService = permissionService;
        _env = env;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // در محیط Development، همه دسترسی‌ها مجاز
        if (_env.IsDevelopment())
        {
            context.Succeed(requirement);
            return;
        }

        // خواندن UserId از Claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            context.Fail();
            return;
        }

        // بررسی مجوز
        var hasPermission = await _permissionService.HasPermissionAsync(
            userId,
            requirement.PermissionName);

        if (hasPermission)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}