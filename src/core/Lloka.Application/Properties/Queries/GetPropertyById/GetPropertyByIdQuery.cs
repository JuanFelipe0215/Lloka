using MediatR;

namespace Lloka.Application.Properties.Queries.GetPropertyById;

public record GetPropertyByIdQuery(Guid PropertyId) : IRequest<PropertyDetailResult>;
