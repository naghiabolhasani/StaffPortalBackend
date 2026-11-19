using System.Threading.Tasks;
using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IStaffSmsService
{
    Task<ReceiveSmsResult> ReceiveSmsSearchAsync(ReceiveSmsRequest request);
    Task<UpdateCustomerInfoResult> UpdateCustomerInfoAsync(
    int id,
    string contractNo,
    long userId);


    Task<SetAlarmResult> SetAlarmAsync(
        int smsId,
        long userId,
        string answeredMessage,
        string contractNo);

    Task SyncAsync();
    Task<DebugSmsResponse> DebugFetchRawAsync(int count = 250);
}