using StaffPortal.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace StaffPortal.Endpoints;

public static class HealthEndpoint
{
    public static void MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
        {
            int buildNumber = CommonMethod.ReadSettingVersion();
            string version = $"0.{buildNumber}";
            string persianDateTime = CommonMethod.GetCurrentPersianDateTime();

            string message = $"Healthy StaffPortal - Version {version} - {persianDateTime}";

            return Results.Ok(new { message });
        })
        .WithName("Health")
        .WithTags("Health")
        .AllowAnonymous()  // ← اضافه شد
        .Produces(200);
    }
}