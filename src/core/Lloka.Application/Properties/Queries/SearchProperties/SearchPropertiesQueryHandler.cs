using Lloka.Application.Common.Interfaces;
using MediatR;

namespace Lloka.Application.Properties.Queries.SearchProperties;

public class SearchPropertiesQueryHandler(IPropertyRepository propertyRepo)
    : IRequestHandler<SearchPropertiesQuery, IReadOnlyList<SearchPropertyResult>>
{
    public async Task<IReadOnlyList<SearchPropertyResult>> Handle(
        SearchPropertiesQuery request, CancellationToken ct)
    {
        var properties = await propertyRepo.SearchAsync(
            request.City,
            request.MinGuests,
            request.CheckIn,
            request.CheckOut,
            request.Page,
            request.PageSize,
            ct);

        return properties
            .Select(p => new SearchPropertyResult(
                p.Id,
                p.Title,
                p.Address.City,
                p.Address.Department,
                p.PricePerNight,
                p.MaxGuests,
                p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url))
            .ToList();
    }
}
