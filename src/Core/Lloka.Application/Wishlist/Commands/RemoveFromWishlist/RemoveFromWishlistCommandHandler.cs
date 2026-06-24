using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Wishlist.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler(
    IWishlistRepository wishlistRepo,
    IUnitOfWork         unitOfWork
) : IRequestHandler<RemoveFromWishlistCommand, Unit>
{
    public async Task<Unit> Handle(RemoveFromWishlistCommand request, CancellationToken ct)
    {
        var item = await wishlistRepo.FindAsync(
            request.PropertyId, request.UserId, request.AnonymousSessionId, ct)
            ?? throw new NotFoundException(nameof(WishlistItem), request.PropertyId);

        wishlistRepo.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
