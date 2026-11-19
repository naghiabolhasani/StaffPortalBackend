using StaffPortal.Models;
using FluentValidation;

namespace StaffPortal.Validators;

public class StaffPermissionsRequestValidator : AbstractValidator<StaffPermissionsRequest>
{
    public StaffPermissionsRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است");
    }
}