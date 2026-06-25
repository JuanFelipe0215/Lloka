using MediatR;

namespace Lloka.Application.Bookings.Queries.GetUserBookings;

public record GetUserBookingsQuery(Guid UserId) : IRequest<IReadOnlyList<BookingListItemResult>>;
