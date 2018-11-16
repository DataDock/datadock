using DataDock.Common.Models;
using FluentValidation;

namespace DataDock.Common.Validators
{
    public class OwnerSettingsValidator: AbstractValidator<OwnerSettings>
    {
        public OwnerSettingsValidator()
        {
            RuleFor(os => os.OwnerId).NotEmpty();
        }
    }
}
