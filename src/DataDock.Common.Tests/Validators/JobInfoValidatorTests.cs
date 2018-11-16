using DataDock.Common.Models;
using DataDock.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DataDock.Common.Tests.Validators
{
    public class JobInfoValidatorTests
    {
        private readonly ImportJobRequestInfoValidator _validator;

        public JobInfoValidatorTests()
        {
            _validator = new ImportJobRequestInfoValidator();
        }

        [Fact]
        public void UserIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.UserId, (string) null);
            _validator.ShouldHaveValidationErrorFor(x => x.UserId, string.Empty);
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
        public void DatasetIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.DatasetId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.DatasetId, string.Empty);
        }

        [Fact]
        public void CsvFileNameMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.CsvFileName, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.CsvFileName, string.Empty);
        }

        [Fact]
        public void CsvFileIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.CsvFileId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.CsvFileId, string.Empty);
        }

        [Fact]
        public void CsvmFileIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.CsvmFileId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.CsvmFileId, string.Empty);
        }

        [Fact]
        public void DatasetIriMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.DatasetIri, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.DatasetIri, string.Empty);
        }

        [Fact]
        public void DatasetIriMustNotBeRelativeIri()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.DatasetIri, "/foo/bar");
        }

        [Fact]
        public void DatasetIriMustBeAbsoluteIri()
        {
            _validator.ShouldNotHaveValidationErrorFor(x => x.DatasetIri, "http://example.org/foo/bar");
        }
    }
}
