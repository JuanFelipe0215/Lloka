using Lloka.Domain.Entities;

namespace Lloka.Application.Common.Interfaces;

public interface IWishlistRepository : IRepository<WishlistItem>
{
    Task<IReadOnlyList<WishlistItem>> GetBySessionIdAsync(
        Guid sessionId, CancellationToken ct = default);

    Task<IReadOnlyList<WishlistItem>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default);

    Task<WishlistItem?> FindAsync(
        Guid propertyId, Guid? userId, Guid? sessionId, CancellationToken ct = default);
}
