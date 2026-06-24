using MediatR;

namespace Lloka.Application.Wishlist.Queries.GetUserWishlist;

public record GetUserWishlistQuery(
    Guid? UserId,
    Guid? AnonymousSessionId
) : IRequest<IReadOnlyList<WishlistItemResult>>;
