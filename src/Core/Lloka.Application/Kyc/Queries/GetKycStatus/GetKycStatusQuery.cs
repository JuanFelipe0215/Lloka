using MediatR;

namespace Lloka.Application.Kyc.Queries.GetKycStatus;

public record GetKycStatusQuery(Guid UserId) : IRequest<GetKycStatusResponse>;
