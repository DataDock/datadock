using System;
using System.Collections.Generic;
using System.Text;
using DataDock.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DataDock.Common.Tests.Validators
{
    public class UserSettingsValidatorTests
    {
        private readonly UserSettingsValidator _validator;

        public UserSettingsValidatorTests()
        {
            _validator = new UserSettingsValidator();
        }

        [Fact]
        public void UserIdMustNotBeNullOrEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(x => x.UserId, (string)null);
            _validator.ShouldHaveValidationErrorFor(x => x.UserId, string.Empty);
        }

        [Fact]
        public void LastModifiedByMayBeNull()
        {
            _validator.ShouldNotHaveValidationErrorFor(x=>x.LastModifiedBy, (string)null);
        }
    }
}
