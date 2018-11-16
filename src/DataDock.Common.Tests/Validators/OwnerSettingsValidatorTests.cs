using System;
using System.Collections.Generic;
using System.Text;
using DataDock.Common.Models;
using DataDock.Common.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DataDock.Common.Tests.Validators
{
    public class OwnerSettingsValidatorTests
    {
        private readonly OwnerSettingsValidator _validator;

        public OwnerSettingsValidatorTests()
        {
            _validator = new OwnerSettingsValidator();
        }

        [Fact]
        public void OwnerIdMustNotBeNull()
        {
            _validator.ShouldHaveValidationErrorFor(o => o.OwnerId, (string)null);
        }

        [Fact]
        public void OwnerIdMustNotBeEmpty()
        {
            _validator.ShouldHaveValidationErrorFor(o => o.OwnerId, string.Empty);
        }

    }
}
