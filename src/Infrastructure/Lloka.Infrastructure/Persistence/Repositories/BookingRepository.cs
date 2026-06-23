using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Lloka.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Lloka.Infrastructure.Persistence.Repositories;

public class BookingRepository(LlokaDbContext context)
    : Repository<Booking>(context), IBookingRepository
{
    public async Task<bool> HasOverlapAsync(
        Guid propertyId,
        StayPeriod stayPeriod,
        CancellationToken ct = default)
    {
        return await Context.Bookings
            .Where(b => b.PropertyId == propertyId
                     && b.Status != BookingStatus.Cancelled
                     && b.StayPeriod.CheckIn  < stayPeriod.CheckOut
                     && b.StayPeriod.CheckOut > stayPeriod.CheckIn)
            .AnyAsync(ct);
    }

    public async Task<bool> HasConfirmedBookingAsync(
        Guid guestId,
        CancellationToken ct = default)
    {
        return await Context.Bookings
            .AnyAsync(b => b.GuestId == guestId
                        && b.Status == BookingStatus.Confirmed, ct);
    }
}
