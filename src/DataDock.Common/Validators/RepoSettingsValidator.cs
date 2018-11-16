using DataDock.Common.Models;
using FluentValidation;

namespace DataDock.Common.Validators
{
    public class RepoSettingsValidator: AbstractValidator<RepoSettings>
    {
        public RepoSettingsValidator()
        {
            RuleFor(rs => rs.OwnerId).NotEmpty();
            RuleFor(rs => rs.RepositoryId).NotEmpty();
        }
    }
}
