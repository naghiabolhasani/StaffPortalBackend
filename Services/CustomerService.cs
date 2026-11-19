using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Common;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class CustomerService : ICustomerService
{
    // ═══════════════════════════════════════════════════════════════
    //  تنظیمات Cache
    // ═══════════════════════════════════════════════════════════════
    private const int CACHE_IDLE_MINUTES = 5;
    private const int CACHE_MAX_MINUTES = 15;
    private const string CACHE_KEY = "customer_service_cache_v2";   // ← v2 (کش جدید به‌خاطر ساختار جدید)
    private const int MAX_RESULTS = 100;

    private static readonly TimeSpan CacheIdleTimeout = TimeSpan.FromMinutes(CACHE_IDLE_MINUTES);
    private static readonly TimeSpan CacheMaxLifetime = TimeSpan.FromMinutes(CACHE_MAX_MINUTES);
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    private readonly string _connectionString;
    private readonly ILogger<CustomerService> _logger;
    private readonly IMemoryCache _cache;

    public CustomerService(
        IConfiguration configuration,
        ILogger<CustomerService> logger,
        IMemoryCache cache)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
        _cache = cache;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Search by Name — حداکثر ۱۰۰ نتیجه
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<CustomerModel>> SearchByNameAsync(string? name)
    {
        var normName = TextNormalizer.NormalizeForSearch(name);
        var allCustomers = await GetOrBuildCacheAsync();

        var query = allCustomers.AsEnumerable();

        if (normName != null)
            query = query.Where(c => c.NormalizedName.Contains(normName));

        return query
            .Take(MAX_RESULTS)
            .Select(c => new CustomerModel
            {
                CustomerName = c.CustomerName,
                NationalCode = c.NationalCode,
                ContractNo = c.ContractNo,
                Tarh = c.Tarh
            })
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ساخت / خواندن Cache
    // ═══════════════════════════════════════════════════════════════
    private async Task<List<CachedCustomer>> GetOrBuildCacheAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY, out List<CachedCustomer>? cached) && cached != null)
        {
            _logger.LogDebug("Customer cache hit. Sliding window reset to {Minutes} min.",
                CACHE_IDLE_MINUTES);
            return cached;
        }

        await _cacheLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(CACHE_KEY, out cached) && cached != null)
                return cached;

            _logger.LogInformation("Building customer cache...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ═══════════════════════════════════════════════════════
            //  کوئرى کامل با JOIN (شامل Contract و Tarh)
            // ═══════════════════════════════════════════════════════
            const string sql = @"
SELECT 
    CU.FName + '  ' + CU.LName  AS CustomerName,
    CU.FName + CU.LName         AS RawName,
    CU.NationalCode             AS NationalCode,
    C.ContractNo                AS ContractNo,
    G.Tarh                      AS Tarh
FROM Leasing.dbo.FUNCustomer CU WITH (NOLOCK)
INNER JOIN Leasing.dbo.FUNContract C WITH (NOLOCK)
    ON C.CifRef = CU.Serial
INNER JOIN DarikAssistant.dbo.ContractGrade G WITH (NOLOCK)
    ON G.ContractRef = C.Serial
WHERE CU.FName IS NOT NULL 
  AND CU.LName IS NOT NULL
  AND CU.NationalCode IS NOT NULL
  AND LTRIM(RTRIM(CU.NationalCode)) <> '';";

            await using var connection = new SqlConnection(_connectionString);
            var raw = (await connection.QueryAsync<CachedCustomer>(sql)).ToList();

            // ─── نرمال‌سازى ───
            foreach (var c in raw)
            {
                c.NormalizedName = TextNormalizer.NormalizeForSearch(c.RawName) ?? "";
            }

            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheIdleTimeout,
                AbsoluteExpirationRelativeToNow = CacheMaxLifetime
            };

            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                var count = (value as List<CachedCustomer>)?.Count ?? 0;
                _logger.LogInformation(
                    "Customer cache evicted: Reason={Reason}, Records={Count}. Memory freed.",
                    reason, count);
            });

            _cache.Set(CACHE_KEY, raw, options);

            sw.Stop();
            _logger.LogInformation(
                "Customer cache built: {Count} records in {Elapsed}ms.",
                raw.Count, sw.ElapsedMilliseconds);

            return raw;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  مدل داخلى Cache
    // ═══════════════════════════════════════════════════════════════
    private class CachedCustomer
    {
        public string? CustomerName { get; set; }
        public string? RawName { get; set; }
        public string? NationalCode { get; set; }
        public string? ContractNo { get; set; }
        public string? Tarh { get; set; }
        public string NormalizedName { get; set; } = "";
    }
}