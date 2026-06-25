using Lloka.Domain.Enums;

namespace Lloka.Application.Bookings.Queries.GetUserBookings;

public record BookingListItemResult(
    Guid           BookingId,
    string         PropertyTitle,
    DateTimeOffset CheckIn,
    DateTimeOffset CheckOut,
    decimal        TotalAmount,
    BookingStatus  Status,
    int            Nights);
