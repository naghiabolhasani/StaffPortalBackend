using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("شناسه کاربر نامعتبر است");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است")
            .MinimumLength(6).WithMessage("رمز عبور باید حداقل ۶ کاراکتر باشد");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تکرار رمز عبور الزامی است")
            .Equal(x => x.Password).WithMessage("رمز عبور و تکرار آن مطابقت ندارند");
    }
}