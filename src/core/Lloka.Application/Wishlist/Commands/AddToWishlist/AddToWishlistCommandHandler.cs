using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Wishlist.Commands.AddToWishlist;

public class AddToWishlistCommandHandler(
    IWishlistRepository  wishlistRepo,
    IPropertyRepository  propertyRepo,
    IUnitOfWork          unitOfWork
) : IRequestHandler<AddToWishlistCommand, AddToWishlistResponse>
{
    public async Task<AddToWishlistResponse> Handle(AddToWishlistCommand request, CancellationToken ct)
    {
        var property = await propertyRepo.GetByIdAsync(request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);

        var existing = await wishlistRepo.FindAsync(
            request.PropertyId, request.UserId, request.AnonymousSessionId, ct);

        if (existing is not null)
            throw new ConflictException("El inmueble ya está en tu lista de favoritos.");

        var item = WishlistItem.Create(property.Id, request.UserId, request.AnonymousSessionId);
        await wishlistRepo.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AddToWishlistResponse(item.Id, item.PropertyId);
    }
}
