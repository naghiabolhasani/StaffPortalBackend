using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است");
    }
}