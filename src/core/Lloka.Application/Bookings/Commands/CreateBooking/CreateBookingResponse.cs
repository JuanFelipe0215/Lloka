using Lloka.Domain.Enums;

namespace Lloka.Application.Bookings.Commands.CreateBooking;

public record CreateBookingResponse(
    Guid           BookingId,
    decimal        TotalAmount,
    DateTimeOffset CheckIn,
    DateTimeOffset CheckOut,
    int            Nights,
    BookingStatus  Status
);
