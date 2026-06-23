using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lloka.Infrastructure.Persistence.Repositories;

public class WishlistRepository(LlokaDbContext context)
    : Repository<WishlistItem>(context), IWishlistRepository
{
    public async Task<IReadOnlyList<WishlistItem>> GetBySessionIdAsync(
        Guid sessionId, CancellationToken ct = default) =>
        await Context.WishlistItems
            .Where(w => w.AnonymousSessionId == sessionId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WishlistItem>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        await Context.WishlistItems
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);

    public async Task<WishlistItem?> FindAsync(
        Guid propertyId, Guid? userId, Guid? sessionId, CancellationToken ct = default) =>
        await Context.WishlistItems.FirstOrDefaultAsync(
            w => w.PropertyId == propertyId
              && (userId.HasValue
                    ? w.UserId             == userId
                    : w.AnonymousSessionId == sessionId),
            ct);
}
