using MediatR;

namespace Lloka.Application.Kyc.Commands.SubmitKycDocument;

public record SubmitKycDocumentCommand(
    Guid   UserId,
    string DocumentBase64
) : IRequest<SubmitKycDocumentResponse>;
