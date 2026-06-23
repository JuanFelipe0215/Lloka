using MediatR;

namespace Lloka.Application.Bookings.Commands.CreateBooking;

public record CreateBookingCommand(
    Guid     PropertyId,
    Guid     GuestId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int      GuestCount,
    Guid?    AnonymousSessionId = null
) : IRequest<CreateBookingResponse>;
