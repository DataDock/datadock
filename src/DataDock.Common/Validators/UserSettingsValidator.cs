using DataDock.Common.Models;
using FluentValidation;

namespace DataDock.Common.Validators
{
    public class UserSettingsValidator: AbstractValidator<UserSettings>
    {
        public UserSettingsValidator()
        {
            RuleFor(u => u.UserId).NotEmpty();
        }
    }
}
