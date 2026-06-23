using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lloka.Infrastructure.Persistence.Repositories;

public class PropertyRepository(LlokaDbContext context)
    : Repository<Property>(context), IPropertyRepository
{
    public async Task<IReadOnlyList<Property>> SearchAsync(
        string?   city,
        int       minGuests,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int       page,
        int       pageSize,
        CancellationToken ct = default)
    {
        var query = Context.Properties
            .Include(p => p.Images)
            .Where(p => p.Status == PropertyStatus.Active)
            .Where(p => p.MaxGuests >= minGuests);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.Address.City.ToLower().Contains(city.ToLower()));

        if (checkIn.HasValue && checkOut.HasValue)
        {
            // Colombia UTC-5: CheckIn 14:00 COT = 19:00 UTC; CheckOut 12:00 COT = 17:00 UTC
            var checkInUtc  = new DateTimeOffset(checkIn.Value.Year,  checkIn.Value.Month,  checkIn.Value.Day,  19, 0, 0, TimeSpan.Zero);
            var checkOutUtc = new DateTimeOffset(checkOut.Value.Year, checkOut.Value.Month, checkOut.Value.Day, 17, 0, 0, TimeSpan.Zero);

            var bookedPropertyIds = Context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled
                         && b.StayPeriod.CheckIn  < checkOutUtc
                         && b.StayPeriod.CheckOut > checkInUtc)
                .Select(b => b.PropertyId);

            query = query.Where(p => !bookedPropertyIds.Contains(p.Id));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
