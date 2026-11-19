using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class StaffLoginRequestValidator : AbstractValidator<StaffLoginRequest>
{
    public StaffLoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است");
    }
}