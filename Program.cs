using System;
using System.Threading.Tasks;
using StaffPortal;
using StaffPortal.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
//    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
//    .WriteTo.Console()
//    .WriteTo.File("Logs/app-.txt", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

// ═══════════════════════════════════════════════════════════════
//  مسیر مطلق براى لاگ‌ها (کنار فایل اجرایى)
// ═══════════════════════════════════════════════════════════════
var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");

try
{
    if (!Directory.Exists(logsPath))
        Directory.CreateDirectory(logsPath);
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Cannot create Logs directory: {ex.Message}");
    logsPath = AppContext.BaseDirectory;   // fallback
}
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsPath, "app-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    CommonMethod.IncrementVersionIfDevelopment(builder.Environment);

    int buildNumber = CommonMethod.ReadSettingVersion();
    string version = $"0.{buildNumber}";

    Console.WriteLine();
    Console.WriteLine();
    Log.Information("========================================");
    Log.Information("StaffPortal API Backend - Build Version {version}", version);
    Log.Information("Started at (Persian): {PersianDateTime}", CommonMethod.GetCurrentPersianDateTime());
    Log.Information("Started at (Gregorian): {GregorianDateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    Log.Information("========================================");
    Log.Information("");

  

    Startup.ConfigureServices(builder);
    var app = builder.Build();

    Startup.ConfigureMiddleware(app);
    Startup.ConfigureEndpoints(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}