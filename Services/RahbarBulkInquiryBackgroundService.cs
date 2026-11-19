using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StaffPortal.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class RahbarBulkInquiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RahbarBulkInquiryBackgroundService> _logger;

    public RahbarBulkInquiryBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RahbarBulkInquiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ═══════════════════════════════════════════════════════════
        //  خواندن تنظیمات
        // ═══════════════════════════════════════════════════════════
        var enabled = _configuration.GetValue<bool>("RahbarBulkInquiry:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("RahbarBulkInquiryBackgroundService is disabled via config.");
            return;
        }
        var runAtTimeStr = _configuration["RahbarBulkInquiry:RunAtTime"] ?? "02:00";
        if (!TimeSpan.TryParse(runAtTimeStr, out var runAt))
        {
            _logger.LogWarning(
                "Invalid RunAtTime '{Time}'. Using default 02:00.",
                runAtTimeStr);
            runAt = TimeSpan.FromHours(2);
        }

        _logger.LogInformation(
            "RahbarBulkInquiryBackgroundService started. Daily run at {RunAt}.",
            runAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ─── محاسبه زمان اجراى بعدى ───
                var now = DateTime.Now;
                var nextRun = now.Date.Add(runAt);
                if (nextRun <= now)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                _logger.LogInformation(
                    "Next bulk inquiry at {NextRun} (in {Hours:F1} hours).",
                    nextRun, delay.TotalHours);
                // ─── صبر تا زمان اجرا ───
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                // ─── اجرا ───
               // _logger.LogInformation("Scheduled bulk inquiry triggered.");

                using var scope = _serviceProvider.CreateScope();
                var rahbarService = scope.ServiceProvider.GetRequiredService<IRahbarService>();

                var result = await rahbarService.RunBulkInquiryAsync(stoppingToken);
                _logger.LogInformation("=================================");

                //_logger.LogInformation(
                //    "Scheduled bulk inquiry completed. Success={Success}, {Message}",
                //    result.IsSuccess, result.Message);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("RahbarBulkInquiryBackgroundService cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in RahbarBulkInquiryBackgroundService. Retrying in 5 minutes.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        _logger.LogInformation(
           "RahbarBulkInquiryBackgroundService stopped at {Time}.",

           DateTime.Now);

        _logger.LogInformation("=================================");
    }
}