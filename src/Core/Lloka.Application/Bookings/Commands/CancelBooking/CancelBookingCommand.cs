using MediatR;

namespace Lloka.Application.Bookings.Commands.CancelBooking;

public record CancelBookingCommand(
    Guid BookingId,
    Guid RequestingUserId
) : IRequest<Unit>;
