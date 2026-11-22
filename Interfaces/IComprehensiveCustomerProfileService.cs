using System.Collections.Generic;
using System.Threading.Tasks;
using StaffPortal.Models;

namespace StaffPortal.Interfaces;

public interface IComprehensiveCustomerProfileService
{
    Task<List<ComprehensiveBaseInfoItem>> GetBaseInfoAsync(
        string? nationalCode,
        string? contractNo);
    Task<SendTemplateSmsResult> SendTemplateSmsAsync(long templateMessageId, string contractNo, long userId);
    Task<SendCustomSmsResult> SendCustomSmsAsync(string message, string contractNo, long userId);
    Task<UpdateRahbarResult> UpdateRahbarAsync(string contractNo, string installmentNo);
    Task<List<ComprehensivePayDetailItem>> GetPayDetailAsync(long contractRef);
    Task<List<ContractDetailItem>> GetContractDetailAsync(long contractRef);
    Task<List<SendSmsItem>> GetSendSmsAsync(long contractRef);
    Task<List<ReceiveSmsItem>> GetReceiveSmsAsync(long contractRef);
    Task<List<DepositItem>> GetDepositAsync(long contractRef);
    Task<List<FunPaymentItem>> GetFunPaymentAsync(long contractRef);
    Task<List<SponserItem>> GetSponserAsync(string contractNo);

    Task<List<TreasuryItem>> GetTreasuryAsync(long contractRef);

    Task<List<SettlementDetailItem>> GetSettlementDetailAsync(long contractRef);   // ← جدید


    Task<List<ChecksItem>> GetChecksAsync(long contractRef);

    Task<List<AlarmsItem>> GetAlarmsAsync(string contractNo);

    Task<InsertAlarmResult> InsertAlarmAsync(
    long userId,
    long contractSerial,
    string message,
    string contactNumber);

    Task<UpdateContactsResult> UpdateContactsAsync(
    string contractNo,
    string? mobile1,
    string? mobile2);
}