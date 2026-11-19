using System.Text.RegularExpressions;
using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class ReceiveSmsRequestValidator : AbstractValidator<ReceiveSmsModels>
{
    public ReceiveSmsRequestValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("تاریخ شروع الزامی است")
            .Matches(@"^\d{4}[/-]\d{2}[/-]\d{2}$")
            .WithMessage("فرمت تاریخ شروع نامعتبر است");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("تاریخ پایان الزامی است")
            .Matches(@"^\d{4}[/-]\d{2}[/-]\d{2}$")
            .WithMessage("فرمت تاریخ پایان نامعتبر است");

        // ===== اعتبارسنجی پیجینگ =====
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 500).WithMessage("تعداد رکورد در هر صفحه باید بین ۱ تا ۵۰۰ باشد");

        // ===== اعتبارسنجی Mobile =====
        //RuleFor(x => x.Mobile)
        //    .Must(BeValidMobileOrEmpty)
        //    .WithMessage("فرمت شماره موبایل نامعتبر است");
    }

    private static bool BeValidMobileOrEmpty(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return true;

        return Regex.IsMatch(mobile, @"^09[0-9]{9}$");
    }
}