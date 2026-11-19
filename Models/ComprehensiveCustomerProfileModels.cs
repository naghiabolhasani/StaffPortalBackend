using System.Collections.Generic;

namespace StaffPortal.Models;

// ===== Request: Base Info =====
public class ComprehensiveBaseInfoRequest
{
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
}

// ===== Response: Base Info (Query 1) =====
public class ComprehensiveBaseInfoItem
{
    public long ContractSerial { get; set; }
    public string? FullName { get; set; }
    public string? NationalCode { get; set; }
    public string? ContractNo { get; set; }
    public string? ContractStatus { get; set; }
    public string? PlanName { get; set; }
    public string? AgnName { get; set; }
    public string? AccountNo { get; set; }
    public string? SourceName { get; set; }

    public long? SourceRef { get; set; }
    public string? InstallmentNo { get; set; }
    public string? Mobile1 { get; set; }
    public string? Mobile2 { get; set; }
}

// ===== Request: Pay Detail =====
public class ComprehensivePayDetailRequest
{
    public long ContractRef { get; set; }
}

// ===== Response: Pay Detail (Query 2) =====
public class ComprehensivePayDetailItem
{
    public string? ContractNo { get; set; }
    public long ContractSerial { get; set; }

    public decimal? Arrear { get; set; }
    public decimal? UnpaidInstallmentCount { get; set; }
    public decimal? TotalDeposit { get; set; }
    public decimal? OldInstallment { get; set; }
    public string? LastpayDate { get; set; }
    public decimal? NextInstallment { get; set; }
    public decimal? ReturnCashAmount { get; set; }
    public int? ReturnCashCount { get; set; }
    public decimal? ReturnCashTajmiAmount { get; set; }
    public int? ReturnCashTajmi { get; set; }
}
// ═══════════════════════════════════════════════════════════════
//  Request مشترک براى چهار اندپوینت جدید
// ═══════════════════════════════════════════════════════════════
public class ContractRefRequest
{
    public long ContractRef { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  SendSms — پیامک‌های ارسالى از Leasing.FUNSMS
// ═══════════════════════════════════════════════════════════════
public class SendSmsItem
{
    public long RowNumber { get; set; }
    public string? SmsText { get; set; }
    public string? SendDate { get; set; }
    public string? SendTime { get; set; }
    public string? Mobile { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  ReceiveSms — پیامک‌هاى دریافتى از DarikAssistant.gen.SmsReport
// ═══════════════════════════════════════════════════════════════
public class ReceiveSmsItem
{
    public long RowNumber { get; set; }
    public string? MobileSender { get; set; }
    public string? SendDate { get; set; }
    public string? SendTime { get; set; }
    public string? MessageText { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Deposit — واریزى / چک / استرداد
// ═══════════════════════════════════════════════════════════════
public class DepositItem
{
    public long RowNumber { get; set; }
    public string? NoteDate { get; set; }
    public string? CashType { get; set; }
    public string? NoteNo { get; set; }
    public string? Amount { get; set; }
    public string? Title { get; set; }
    public string? Dsr { get; set; }
    public string? BankTitle { get; set; }
    public string? BankAccNo { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  FunPayment — اقساط قرارداد
// ═══════════════════════════════════════════════════════════════
public class FunPaymentItem
{
    public long RowNumber { get; set; }
    public string? ShouldPayDate { get; set; }
    public string? InstallmentAmount { get; set; }
    public decimal? PayAmount { get; set; }
    public string? PayDate { get; set; }
    public string? Creditor { get; set; }
    public string? Color { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Sponser — اطلاعات راهبر / تامین‌کننده مالى
// ═══════════════════════════════════════════════════════════════
public class SponserRequest
{
    public string? ContractNo { get; set; }
}

public class SponserItem
{
    public string? SponserName { get; set; }
    public long? ArrearsInstallmentCount { get; set; }
    public decimal? TotalArrearsAmount { get; set; }
    public string? UpdateDate { get; set; }

    public string? UpdateTime { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Treasury — خزانه
// ═══════════════════════════════════════════════════════════════
public class TreasuryItem
{
    public long RowNumber { get; set; }
    public string? Title { get; set; }
    public string? NoteDate { get; set; }
    public string? NoteNo { get; set; }
    public string? SayadNoteNo { get; set; }
    public decimal? Amount { get; set; }
    public string? BankTitle { get; set; }
    public string? BankAccNo { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Checks — چک‌ها
// ═══════════════════════════════════════════════════════════════
public class ChecksItem
{
    public long RowNumber { get; set; }
    public string? CheckKind { get; set; }
    public string? Behalf { get; set; }
    public string? NoteDate { get; set; }
    public string? NoteNo { get; set; }
    public string? SayadNoteNo { get; set; }
    public decimal? Amount { get; set; }
    public string? CheckStatus { get; set; }
    public string? Dsr { get; set; }              // ← 🆕 (شرح)
    public string? BankName { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Alarms — هشدارها
// ═══════════════════════════════════════════════════════════════
public class AlarmsRequest
{
    public string? ContractNo { get; set; }
}

public class AlarmsItem
{
    public long RowNumber { get; set; }
    public string? AlarmTypeTitle { get; set; }
    public string? AlarmDsr { get; set; }
    public string? AlarmDate { get; set; }
    public string? AlarmTime { get; set; }
    public string? ContactNumber { get; set; }
    public string? AlarmStatusTitle { get; set; }
    public string? UserName { get; set; }
}




// ═══════════════════════════════════════════════════════════════
//  Insert Alarm — ثبت هشدار جدید
// ═══════════════════════════════════════════════════════════════
public class InsertAlarmRequest
{
    public long ContractSerial { get; set; }
    public string? Message { get; set; }
    public string? ContactNumber { get; set; }
}

public class InsertAlarmResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}


// ═══════════════════════════════════════════════════════════════
//  Update Contacts — به‌روزرسانی شماره تماس مشتری
// ═══════════════════════════════════════════════════════════════
public class UpdateContactsRequest
{
    public string? ContractNo { get; set; }
    public string? Mobile1 { get; set; }
    public string? Mobile2 { get; set; }
}

public class UpdateContactsResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int AffectedRows { get; set; }
}

public class ContractDetailItem
{
    public string? PursuerName { get; set; }           // نام پیگیری‌کننده
    public decimal? SumDebt { get; set; }              // اصل تسهیلات
    public decimal? Profit { get; set; }               // فرع تسهیلات
    public decimal? Rate { get; set; }                 // نرخ
    public int? Duration { get; set; }                 // مدت
    public int? Step { get; set; }                     // گام کاهش
    public int? InstallmentCount { get; set; }         // تعداد اقساط (Duration / Step)
}

public class SettlementDetailItem
{
    public string? ContractNo { get; set; }
    public long ContractSerial { get; set; }

    public decimal? InsuranceIssuedAmount { get; set; }

    public decimal? Penalty { get; set; }
    public decimal? ClearAmount { get; set; }
    public decimal? UnpaidPenaltyAmount { get; set; }
    public decimal? FinalSettlementAmount { get; set; }
    public decimal? KarmozdVosoolMotalebat { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Request
// ═══════════════════════════════════════════════════════════════
public class SendTemplateSmsRequest
{
    public long TemplateMessageId { get; set; }
    public string? ContractNo { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Response
// ═══════════════════════════════════════════════════════════════
public class SendTemplateSmsResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public long TemplateMessageId { get; set; }
    public string? ContractNo { get; set; }
    public string? FinalMessage { get; set; }
}