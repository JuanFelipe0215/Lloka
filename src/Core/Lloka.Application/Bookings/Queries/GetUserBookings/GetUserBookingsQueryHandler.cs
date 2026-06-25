using Lloka.Application.Common.Interfaces;
using MediatR;

namespace Lloka.Application.Bookings.Queries.GetUserBookings;

public class GetUserBookingsQueryHandler(IBookingRepository bookingRepo)
    : IRequestHandler<GetUserBookingsQuery, IReadOnlyList<BookingListItemResult>>
{
    public Task<IReadOnlyList<BookingListItemResult>> Handle(
        GetUserBookingsQuery request, CancellationToken ct) =>
        bookingRepo.GetUserBookingsAsync(request.UserId, ct);
}
