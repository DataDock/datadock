using DataDock.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DataDock.Common.Tests.Validators
{

    public class RepoSettingsValidatorTests
    {
        private readonly RepoSettingsValidator _validator;

        public RepoSettingsValidatorTests()
        {
            _validator = new RepoSettingsValidator();
        }

        [Fact]
        public void OwnerIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.OwnerId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.OwnerId, string.Empty);
        }

        [Fact]
        public void RepositoryIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.RepositoryId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.RepositoryId, string.Empty);
        }
    }
}
