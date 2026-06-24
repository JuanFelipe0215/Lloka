using Ardalis.GuardClauses;
using Lloka.Domain.Common;

namespace Lloka.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public Guid    PropertyId         { get; private set; }
    public Guid?   UserId             { get; private set; }
    public Guid?   AnonymousSessionId { get; private set; }
    public DateTime CreatedAt         { get; private set; }

    private WishlistItem(Guid id, Guid propertyId, Guid? userId, Guid? anonymousSessionId)
        : base(id)
    {
        PropertyId         = propertyId;
        UserId             = userId;
        AnonymousSessionId = anonymousSessionId;
        CreatedAt          = DateTime.UtcNow;
    }

    private protected WishlistItem() { }

    public static WishlistItem Create(Guid propertyId, Guid? userId, Guid? anonymousSessionId)
    {
        Guard.Against.Default(propertyId, nameof(propertyId));

        if (userId is null && anonymousSessionId is null)
            throw new DomainException(
                "Un WishlistItem debe estar asociado a un UserId o un AnonymousSessionId.");

        return new WishlistItem(Guid.NewGuid(), propertyId, userId, anonymousSessionId);
    }

    // Llamado por MergeAnonymousSessionCommand al momento del login/registro.
    public void AssignToUser(Guid userId)
    {
        Guard.Against.Default(userId, nameof(userId));
        UserId             = userId;
        AnonymousSessionId = null;
    }
}
