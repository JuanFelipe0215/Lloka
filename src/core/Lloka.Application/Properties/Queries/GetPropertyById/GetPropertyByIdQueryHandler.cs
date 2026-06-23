using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Properties.Queries.GetPropertyById;

public class GetPropertyByIdQueryHandler(IPropertyRepository propertyRepo)
    : IRequestHandler<GetPropertyByIdQuery, PropertyDetailResult>
{
    public async Task<PropertyDetailResult> Handle(GetPropertyByIdQuery request, CancellationToken ct)
    {
        var property = await propertyRepo.GetByIdAsync(request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(Property), request.PropertyId);

        return new PropertyDetailResult(
            property.Id,
            property.OwnerId,
            property.Title,
            property.Description,
            property.Address.Street,
            property.Address.City,
            property.Address.Department,
            property.Address.Country,
            property.Address.Latitude,
            property.Address.Longitude,
            property.Address.PostalCode,
            property.PricePerNight,
            property.MaxGuests,
            property.Status.ToString(),
            property.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).ToList());
    }
}
