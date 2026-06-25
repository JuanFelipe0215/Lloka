using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Kyc.Queries.GetKycStatus;

public class GetKycStatusQueryHandler(IUserRepository userRepo)
    : IRequestHandler<GetKycStatusQuery, GetKycStatusResponse>
{
    public async Task<GetKycStatusResponse> Handle(GetKycStatusQuery request, CancellationToken ct)
    {
        var user = await userRepo.GetByIdWithKycAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var latest = user.KycVerifications
            .OrderByDescending(v => v.SubmittedAt)
            .FirstOrDefault();

        var summary = latest is null
            ? null
            : new KycVerificationSummary(latest.Id, latest.Status, latest.SubmittedAt, latest.ReviewedAt);

        return new GetKycStatusResponse(user.KycStatus, summary);
    }
}
