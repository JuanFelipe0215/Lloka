using MediatR;

namespace Lloka.Application.Wishlist.Commands.AddToWishlist;

public record AddToWishlistCommand(
    Guid  PropertyId,
    Guid? UserId,
    Guid? AnonymousSessionId
) : IRequest<AddToWishlistResponse>;
