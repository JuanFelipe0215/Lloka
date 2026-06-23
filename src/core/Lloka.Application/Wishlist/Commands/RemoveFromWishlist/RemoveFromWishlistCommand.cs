using MediatR;

namespace Lloka.Application.Wishlist.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(
    Guid  PropertyId,
    Guid? UserId,
    Guid? AnonymousSessionId
) : IRequest<Unit>;
