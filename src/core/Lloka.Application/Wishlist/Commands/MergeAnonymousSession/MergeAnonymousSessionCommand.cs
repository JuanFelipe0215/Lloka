using MediatR;

namespace Lloka.Application.Wishlist.Commands.MergeAnonymousSession;

public record MergeAnonymousSessionCommand(
    Guid UserId,
    Guid AnonymousSessionId
) : IRequest<Unit>;
