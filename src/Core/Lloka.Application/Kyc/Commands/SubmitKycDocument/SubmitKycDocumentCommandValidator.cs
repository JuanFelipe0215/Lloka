using FluentValidation;

namespace Lloka.Application.Kyc.Commands.SubmitKycDocument;

public class SubmitKycDocumentCommandValidator : AbstractValidator<SubmitKycDocumentCommand>
{
    public SubmitKycDocumentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DocumentBase64).NotEmpty();
    }
}
