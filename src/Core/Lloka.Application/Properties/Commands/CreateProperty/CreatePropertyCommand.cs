using MediatR;

namespace Lloka.Application.Properties.Commands.CreateProperty;

public record CreatePropertyCommand(
    Guid    OwnerId,
    string  Title,
    string  Description,
    string  Street,
    string  City,
    string  Department,
    string  Country,
    decimal Latitude,
    decimal Longitude,
    string? PostalCode,
    decimal PricePerNight,
    int     MaxGuests
) : IRequest<CreatePropertyResponse>;
