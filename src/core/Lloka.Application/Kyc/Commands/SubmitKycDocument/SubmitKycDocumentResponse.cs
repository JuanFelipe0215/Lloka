using Lloka.Domain.Enums;

namespace Lloka.Application.Kyc.Commands.SubmitKycDocument;

public record SubmitKycDocumentResponse(
    Guid      VerificationId,
    KycStatus KycStatus,
    string    Message
);
