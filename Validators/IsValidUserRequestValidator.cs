using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class IsValidUserRequestValidator : AbstractValidator<UserExistRequest>
{
    public IsValidUserRequestValidator()
    {
        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Length(11).WithMessage("شماره موبایل باید ۱۱ رقم باشد")
            .Matches(@"^09[0-9]{9}$").WithMessage("شماره موبایل باید با 09 شروع شود و ۱۱ رقم باشد");
    }
}