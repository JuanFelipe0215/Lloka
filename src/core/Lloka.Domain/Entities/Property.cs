using Ardalis.GuardClauses;
using Lloka.Domain.Common;
using Lloka.Domain.Enums;
using Lloka.Domain.ValueObjects;

namespace Lloka.Domain.Entities;

public class Property : BaseEntity
{
    public Guid           OwnerId       { get; private set; }
    public string         Title         { get; private set; } = null!;
    public string         Description   { get; private set; } = null!;
    public Address        Address       { get; private set; } = null!;
    public decimal        PricePerNight { get; private set; }
    public int            MaxGuests     { get; private set; }
    public PropertyStatus Status        { get; private set; }
    public DateTime       CreatedAt     { get; private set; }
    public DateTime       UpdatedAt     { get; private set; }

    private readonly List<PropertyImage> _images = [];
    public IReadOnlyCollection<PropertyImage> Images => _images.AsReadOnly();

    private Property(
        Guid id,
        Guid ownerId,
        string title,
        string description,
        Address address,
        decimal pricePerNight,
        int maxGuests) : base(id)
    {
        OwnerId       = ownerId;
        Title         = title;
        Description   = description;
        Address       = address;
        PricePerNight = pricePerNight;
        MaxGuests     = maxGuests;
        Status        = PropertyStatus.Active;
        CreatedAt     = DateTime.UtcNow;
        UpdatedAt     = DateTime.UtcNow;
    }

    private protected Property() { }

    public static Property Create(
        Guid ownerId,
        string title,
        string description,
        Address address,
        decimal pricePerNight,
        int maxGuests)
    {
        Guard.Against.Default(ownerId,          nameof(ownerId));
        Guard.Against.NullOrWhiteSpace(title,       nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.Null(address,             nameof(address));
        Guard.Against.Negative(pricePerNight,   nameof(pricePerNight));
        Guard.Against.NegativeOrZero(maxGuests, nameof(maxGuests));

        return new Property(Guid.NewGuid(), ownerId, title, description,
                            address, pricePerNight, maxGuests);
    }

    public void AddImage(string url, int displayOrder)
    {
        _images.Add(PropertyImage.Create(Id, url, displayOrder));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        Guard.Against.Negative(newPrice, nameof(newPrice));
        PricePerNight = newPrice;
        UpdatedAt     = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status    = PropertyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status    = PropertyStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string description, decimal pricePerNight, int maxGuests)
    {
        Guard.Against.NullOrWhiteSpace(title,       nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.Negative(pricePerNight,       nameof(pricePerNight));
        Guard.Against.NegativeOrZero(maxGuests,     nameof(maxGuests));

        Title         = title;
        Description   = description;
        PricePerNight = pricePerNight;
        MaxGuests     = maxGuests;
        UpdatedAt     = DateTime.UtcNow;
    }
}
