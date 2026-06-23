namespace Lloka.Application.Wishlist.Queries.GetUserWishlist;

public record WishlistItemResult(
    Guid    WishlistItemId,
    Guid    PropertyId,
    string  PropertyTitle,
    string  City,
    decimal PricePerNight,
    string? ThumbnailUrl
);
