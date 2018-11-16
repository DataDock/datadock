using DataDock.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DataDock.Common.Tests.Validators
{
    public class SchemaInfoValidatorTests
    {
        private readonly SchemaInfoValidator _validator;

        public SchemaInfoValidatorTests()
        {
            _validator = new SchemaInfoValidator();
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

        [Fact]
        public void SchemaIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.SchemaId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.SchemaId, string.Empty);
        }
    }
}
