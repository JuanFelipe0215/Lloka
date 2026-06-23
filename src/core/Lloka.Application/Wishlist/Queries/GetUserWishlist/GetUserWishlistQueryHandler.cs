using Lloka.Application.Common.Interfaces;
using MediatR;

namespace Lloka.Application.Wishlist.Queries.GetUserWishlist;

public class GetUserWishlistQueryHandler(
    IWishlistRepository  wishlistRepo,
    IPropertyRepository  propertyRepo
) : IRequestHandler<GetUserWishlistQuery, IReadOnlyList<WishlistItemResult>>
{
    public async Task<IReadOnlyList<WishlistItemResult>> Handle(
        GetUserWishlistQuery request, CancellationToken ct)
    {
        var items = request.UserId.HasValue
            ? await wishlistRepo.GetByUserIdAsync(request.UserId.Value, ct)
            : await wishlistRepo.GetBySessionIdAsync(request.AnonymousSessionId!.Value, ct);

        var results = new List<WishlistItemResult>(items.Count);
        foreach (var item in items)
        {
            var property = await propertyRepo.GetByIdAsync(item.PropertyId, ct);
            if (property is null) continue;

            results.Add(new WishlistItemResult(
                item.Id,
                property.Id,
                property.Title,
                property.Address.City,
                property.PricePerNight,
                property.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url));
        }

        return results;
    }
}
