using System.Threading.Tasks;
using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IStaffAuthService
{
    Task<(bool IsSuccess, string ErrorMessage, int StatusCode, LoginModels? Data)> LoginAsync(
        StaffLoginRequest request);
}