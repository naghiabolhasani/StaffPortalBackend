using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class UserByUsernameRequestValidator : AbstractValidator<UserByUsernameRequest>
{
    public UserByUsernameRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است");
    }
}