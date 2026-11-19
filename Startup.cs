using StaffPortal.Auth;
using StaffPortal.Data;
using StaffPortal.Endpoints;
using StaffPortal.Interfaces;
using StaffPortal.Middlewares;
using StaffPortal.Models;
using StaffPortal.Services;
using StaffPortal.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SuperAppKamanAPI.Middlewares;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaffPortal;

public static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // ===== Swagger =====
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StaffPortal API",
                Version = "v1",
                Description = "Build Number: v1"
            });

            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "لطفاً ApiKey را در هدر وارد کنید. مثال: 'my-secret-key-123'",
                In = ParameterLocation.Header,
                Name = "X-API-KEY",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });


        services.AddHostedService<RahbarBulkInquiryBackgroundService>();
        services.AddScoped<IComprehensiveCustomerProfileService, ComprehensiveCustomerProfileService>();
        services.AddScoped<IValidator<SetPasswordRequest>, SetPasswordRequestValidator>();
        services.AddScoped<ISimilarCustomerService, SimilarCustomerService>();
        services.AddHostedService<SmsSyncBackgroundService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IGeneralService, GeneralService>();
        // ===== Authentication (ApiKey) =====
        services.AddAuthentication("ApiKey")
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
        services.AddScoped<IRahbarService, RahbarService>();
        // ===== کش =====
        services.AddMemoryCache();

        // ===== Permission Service =====
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ===== Authorization (Permission) =====
        services.AddAuthorization();

        // ===== HttpClient =====
        services.AddHttpClient();

        // ===== Staff Services =====
        services.AddScoped<IStaffAuthService, StaffAuthService>();
        services.AddScoped<IValidator<StaffLoginRequest>, StaffLoginRequestValidator>();

        services.AddScoped<IStaffSmsService, RecieveSmsService>();
        services.AddScoped<IValidator<ReceiveSmsModels>, ReceiveSmsRequestValidator>();


        services.AddScoped<IDynamicReportService, DynamicReportService>();

        services.AddScoped<IContractFacilityService, ContractFacilityService>();

      

        services.AddScoped<IValidator<StaffPermissionsRequest>, StaffPermissionsRequestValidator>();

        // ===== Logging =====
        services.AddScoped<IApiLogService, ApiLogService>();

        // ===== DbContext =====
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // ===== CORS =====
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        services.AddHttpContextAccessor();
    }

    public static void ConfigureMiddleware(WebApplication app)
    {
        app.UseCors("AllowAll");

        app.UseMiddleware<PersianNormalizationMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<DevelopmentAuthenticationMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<ApiLoggingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "StaffPortal API v1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
        }
    }

    public static void ConfigureEndpoints(WebApplication app)
    {
        app.MapHealthEndpoint();
        app.MapUserEndpoints();
        app.MapReceiveSmsEndpoints();
        app.MapComprehensiveCustomerProfileEndpoints();  // ← جدید

        app.MapDynamicReportEndpoints();
        app.MapStaffContractFacilityEndpoints();
        app.MapSimilarCustomerEndpoints();
        app.MapCustomerEndpoints();
        app.MapGeneralEndpoints();
        app.MapRahbarEndpoints();
    }
}