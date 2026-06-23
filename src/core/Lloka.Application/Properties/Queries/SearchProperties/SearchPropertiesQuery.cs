using MediatR;

namespace Lloka.Application.Properties.Queries.SearchProperties;

public record SearchPropertiesQuery(
    string?  City      = null,
    int      MinGuests = 1,
    DateOnly? CheckIn  = null,
    DateOnly? CheckOut = null,
    int      Page      = 1,
    int      PageSize  = 20
) : IRequest<IReadOnlyList<SearchPropertyResult>>;
