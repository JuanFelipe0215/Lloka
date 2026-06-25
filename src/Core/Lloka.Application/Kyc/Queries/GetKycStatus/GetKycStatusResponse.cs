using Lloka.Domain.Enums;

namespace Lloka.Application.Kyc.Queries.GetKycStatus;

public record GetKycStatusResponse(
    KycStatus              KycStatus,
    KycVerificationSummary? LatestVerification);

public record KycVerificationSummary(
    Guid                  VerificationId,
    KycVerificationStatus Status,
    DateTime              SubmittedAt,
    DateTime?             ReviewedAt);
