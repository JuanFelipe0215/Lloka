using Ardalis.GuardClauses;
using Lloka.Domain.Common;
using Lloka.Domain.Enums;
using Lloka.Domain.ValueObjects;

namespace Lloka.Domain.Entities;

public class Booking : BaseEntity
{
    public Guid          PropertyId         { get; private set; }
    public Guid          GuestId            { get; private set; }
    public StayPeriod    StayPeriod         { get; private set; } = null!;
    // Snapshot del precio pactado al crear la reserva.
    // No se recalcula aunque Property.PricePerNight cambie después.
    public decimal       TotalAmount        { get; private set; }
    public BookingStatus Status             { get; private set; }
    public int           GuestCount         { get; private set; }
    public Guid?         AnonymousSessionId { get; private set; }
    public DateTime      CreatedAt          { get; private set; }
    public DateTime      UpdatedAt          { get; private set; }

    private Booking(
        Guid id,
        Guid propertyId,
        Guid guestId,
        StayPeriod stayPeriod,
        decimal totalAmount,
        int guestCount,
        Guid? anonymousSessionId) : base(id)
    {
        PropertyId         = propertyId;
        GuestId            = guestId;
        StayPeriod         = stayPeriod;
        TotalAmount        = totalAmount;
        GuestCount         = guestCount;
        AnonymousSessionId = anonymousSessionId;
        Status             = BookingStatus.Pending;
        CreatedAt          = DateTime.UtcNow;
        UpdatedAt          = DateTime.UtcNow;
    }

    private protected Booking() { }

    public static Booking Create(
        Guid propertyId,
        Guid guestId,
        StayPeriod stayPeriod,
        decimal pricePerNight,
        int guestCount,
        Guid? anonymousSessionId = null)
    {
        Guard.Against.Default(propertyId,       nameof(propertyId));
        Guard.Against.Default(guestId,          nameof(guestId));
        Guard.Against.Null(stayPeriod,          nameof(stayPeriod));
        Guard.Against.NegativeOrZero(guestCount, nameof(guestCount));

        // TotalAmount se calcula desde el VO — nunca lo pasa el caller.
        // Garantiza que el monto siempre sea consistente con el precio y las noches.
        var totalAmount = stayPeriod.CalculateTotalAmount(pricePerNight);

        return new Booking(Guid.NewGuid(), propertyId, guestId,
                           stayPeriod, totalAmount, guestCount, anonymousSessionId);
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException(
                $"Solo una reserva en estado Pending puede confirmarse. Estado actual: {Status}.");

        Status    = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("La reserva ya está cancelada.");

        Status    = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
