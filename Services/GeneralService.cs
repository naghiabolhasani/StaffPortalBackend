using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StaffPortal.Interfaces;
using StaffPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StaffPortal.Services;

public class GeneralService : IGeneralService
{
    private readonly string _connectionString;
    private readonly ILogger<GeneralService> _logger;

    public GeneralService(
        IConfiguration configuration,
        ILogger<GeneralService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  دریافت پیام‌هاى قالب بر اساس Kind + فیلتر Message
    // ═══════════════════════════════════════════════════════════
    public async Task<List<GeneralTemplateMessageResponse>> GetTemplateMessagesAsync(
        int kind,
        string? message = null)
    {
        // ۱. کوئرى پایه — با Id
        var sql = @"
SELECT 
    [id]        AS Id,
    [Message]   AS Message
FROM [DarikAssistant].[app].[MesssageTemplate] WITH (NOLOCK)
WHERE [Active] = 1 
  AND [KindConstant] = @Kind";

        // ۲. شرط LIKE (اگر message ارسال شده)
        if (!string.IsNullOrWhiteSpace(message))
        {
            sql += " AND [Message] LIKE @Message";
            message = $"%{message.Trim()}%";
        }

        // ۳. مرتب‌سازى
        sql += " ORDER BY [Sort];";

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<GeneralTemplateMessageResponse>(
            sql,
            new
            {
                Kind = kind,
                Message = message
            });

        return result.ToList();
    }

}