using Lloka.Domain.Entities;

namespace Lloka.Application.Common.Interfaces;

public interface IPropertyRepository : IRepository<Property>
{
    Task<IReadOnlyList<Property>> SearchAsync(
        string?  city,
        int      minGuests,
        DateOnly? checkIn,
        DateOnly? checkOut,
        int      page,
        int      pageSize,
        CancellationToken ct = default);
}
