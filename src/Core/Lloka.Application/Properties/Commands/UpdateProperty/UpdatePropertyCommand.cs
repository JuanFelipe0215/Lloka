using MediatR;

namespace Lloka.Application.Properties.Commands.UpdateProperty;

public record UpdatePropertyCommand(
    Guid    PropertyId,
    Guid    OwnerId,
    string  Title,
    string  Description,
    decimal PricePerNight,
    int     MaxGuests
) : IRequest<Unit>;
