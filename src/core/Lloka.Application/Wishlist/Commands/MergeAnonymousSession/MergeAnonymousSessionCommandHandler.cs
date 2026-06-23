using Lloka.Application.Common.Interfaces;
using MediatR;

namespace Lloka.Application.Wishlist.Commands.MergeAnonymousSession;

public class MergeAnonymousSessionCommandHandler(
    IWishlistRepository wishlistRepo,
    IUnitOfWork         unitOfWork
) : IRequestHandler<MergeAnonymousSessionCommand, Unit>
{
    public async Task<Unit> Handle(MergeAnonymousSessionCommand request, CancellationToken ct)
    {
        var items = await wishlistRepo.GetBySessionIdAsync(request.AnonymousSessionId, ct);

        if (items.Count == 0)
            return Unit.Value;

        foreach (var item in items)
            item.AssignToUser(request.UserId);

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
