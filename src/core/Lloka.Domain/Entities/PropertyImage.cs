using Ardalis.GuardClauses;
using Lloka.Domain.Common;

namespace Lloka.Domain.Entities;

public class PropertyImage : BaseEntity
{
    public Guid   PropertyId   { get; private set; }
    public string Url          { get; private set; } = null!;
    public int    DisplayOrder { get; private set; }

    private PropertyImage(Guid id, Guid propertyId, string url, int displayOrder)
        : base(id)
    {
        PropertyId   = propertyId;
        Url          = url;
        DisplayOrder = displayOrder;
    }

    private protected PropertyImage() { }

    // Solo Property (mismo ensamblado) puede crear imágenes.
    internal static PropertyImage Create(Guid propertyId, string url, int displayOrder)
    {
        Guard.Against.Default(propertyId,  nameof(propertyId));
        Guard.Against.NullOrWhiteSpace(url, nameof(url));
        Guard.Against.Negative(displayOrder, nameof(displayOrder));

        return new PropertyImage(Guid.NewGuid(), propertyId, url, displayOrder);
    }
}
