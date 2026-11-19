using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace StaffPortal.Auth;

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permission)
    {
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return builder.RequireAuthorization(policy);
    }
}