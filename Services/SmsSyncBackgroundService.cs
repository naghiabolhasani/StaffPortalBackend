using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StaffPortal.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class SmsSyncBackgroundService : BackgroundService
{
    // ═══════════════════════════════════════════════════════════════
    //  تنظیمات زمان‌بندى
    // ═══════════════════════════════════════════════════════════════
    private const int BusinessHourStart = 7;        // ۷ صبح
    private const int BusinessHourEnd = 17;         // ۵ عصر (17:00)
    private const int BusinessIntervalMinutes = 10; // هر ۱۰ دقیقه در ساعات کارى
    private const int OffHourIntervalMinutes = 60;  // هر ۱ ساعت در خارج ساعات کارى

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsSyncBackgroundService> _logger;

    public SmsSyncBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SmsSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    //  محاسبه فاصله تا اجراى بعدى
    // ═══════════════════════════════════════════════════════════════
    private static TimeSpan GetNextInterval()
    {
        var now = DateTime.Now;
        int currentHour = now.Hour;

        // اگر در ساعات کارى هستیم (07:00 تا 16:59)
        if (currentHour >= BusinessHourStart && currentHour < BusinessHourEnd)
        {
            return TimeSpan.FromMinutes(BusinessIntervalMinutes);   // 10 دقیقه
        }

        return TimeSpan.FromMinutes(OffHourIntervalMinutes);        // 60 دقیقه
    }

    // ═══════════════════════════════════════════════════════════════
    //  اجرا
    // ═══════════════════════════════════════════════════════════════
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SmsSyncBackgroundService started. " +
            "Business hours ({Start}:00-{End}:00): every {BusinessMin} min. " +
            "Off hours: every {OffMin} min.",
            BusinessHourStart, BusinessHourEnd,
            BusinessIntervalMinutes, OffHourIntervalMinutes);

        // ─── تأخیر اولیه ۳۰ ثانیه تا برنامه کامل بالا بیاید ───
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ─── اجرا ───
                using var scope = _serviceProvider.CreateScope();
                var smsService = scope.ServiceProvider.GetRequiredService<IStaffSmsService>();

                await smsService.SyncAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SMS sync background service");
            }

            // ─── محاسبه فاصله تا اجراى بعدى ───
            var interval = GetNextInterval();
            var nextRun = DateTime.Now.Add(interval);

            _logger.LogInformation(
                "Next SMS sync at {NextRun} (in {Minutes} min).",
                nextRun.ToString("HH:mm:ss"),
                (int)interval.TotalMinutes);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SmsSyncBackgroundService stopped at {Time}.",
            DateTime.Now);
    }
}