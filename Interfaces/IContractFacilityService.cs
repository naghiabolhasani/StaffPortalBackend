using System.Threading.Tasks;
using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IContractFacilityService
{
    Task<ContractFacilityDto?> SearchAsync(string contractNo);
    Task<bool> UpdateFacilityIdAsync(string contractNo, string facilityId);
    Task<bool> UpdateRatificationDateAsync(string contractNo, string ratificationDate);
}