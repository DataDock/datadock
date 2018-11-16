using System;
using DataDock.Common.Models;
using FluentValidation;

namespace DataDock.Common.Validators
{
    public class JobRequestInfoValidator<T> : AbstractValidator<T> where T:JobRequestInfo
    {
        public JobRequestInfoValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.RepositoryId).NotEmpty();
            RuleFor(x => x.JobType).IsInEnum();
        }
    }

    public class ImportJobRequestInfoValidator : JobRequestInfoValidator<ImportJobRequestInfo>
    {
        public ImportJobRequestInfoValidator()
        {
            RuleFor(x => x.DatasetId).NotEmpty();
            RuleFor(x => x.DatasetIri).NotEmpty().Must(iri=>Uri.IsWellFormedUriString(iri, UriKind.Absolute)).WithMessage("Value must be an absolute URI");
            RuleFor(x => x.CsvFileName).NotEmpty();
            RuleFor(x => x.CsvFileId).NotEmpty();
            RuleFor(x => x.CsvmFileId).NotEmpty();
        }
    }
}
