using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StaffPortal.Auth;

/// <summary>
/// این فیلتر، مقدار پیش‌فرض را برای هدر ApiKey در Swagger UI تنظیم می‌کند
/// </summary>
public class AddDefaultApiKeyHeaderFilter : IOperationFilter
{
    private readonly IConfiguration _configuration;

    public AddDefaultApiKeyHeaderFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // اگر عملیات قبلاً پارامتری با نام X-API-KEY ندارد، آن را اضافه کن
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        // بررسی وجود پارامتر هدر
        var existingParam = operation.Parameters
            .FirstOrDefault(p => p.Name == "X-API-KEY" && p.In == ParameterLocation.Header);

        if (existingParam == null)
        {
            // خواندن مقدار پیش‌فرض از appsettings.json
            var defaultApiKey = _configuration["ApiKey"] ?? "my-secret-key-123";

            // اضافه کردن پارامتر هدر با مقدار پیش‌فرض
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-API-KEY",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new Microsoft.OpenApi.Any.OpenApiString(defaultApiKey) // مقدار پیش‌فرض
                },
                Description = "ApiKey برای احراز هویت"
            });
        }
        else
        {
            // اگر پارامتر وجود دارد، مقدار پیش‌فرض را به‌روز کن
            var defaultApiKey = _configuration["ApiKey"] ?? "my-secret-key-123";
            existingParam.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(defaultApiKey);
        }
    }
}