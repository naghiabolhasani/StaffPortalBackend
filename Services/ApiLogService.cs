using System;
using System.Threading.Tasks;
using StaffPortal.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StaffPortal.Services;

public class ApiLogService : IApiLogService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiLogService> _logger;

    public ApiLogService(
        IConfiguration configuration,
        ILogger<ApiLogService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EditLogAsync(
        string apiName,
        string? inputJson,
        string? outputJson,
        string userIp,
        int? userId = null)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("ApiLog: ConnectionString not found.");
                return;
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO [dbo].[ApiLog]
                    ([ApiName], [InputJson], [OutputJson], [UserIp], [UserId], [CreatedAt])
                VALUES
                    (@ApiName, @InputJson, @OutputJson, @UserIp, @UserId, GETDATE())";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ApiName", apiName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@InputJson", (object?)inputJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@OutputJson", (object?)outputJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserIp", userIp ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing ApiLog for {ApiName}", apiName);
        }
    }
}