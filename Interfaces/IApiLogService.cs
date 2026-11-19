using System.Threading.Tasks;

namespace StaffPortal.Interfaces;

public interface IApiLogService
{
    Task EditLogAsync(
        string apiName,
        string? inputJson,
        string? outputJson,
        string userIp,
        int? userId = null);   // ← اضافه شد
}