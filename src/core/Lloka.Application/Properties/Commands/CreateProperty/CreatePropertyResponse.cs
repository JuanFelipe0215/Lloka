namespace Lloka.Application.Properties.Commands.CreateProperty;

public record CreatePropertyResponse(
    Guid    PropertyId,
    string  Title,
    string  City,
    decimal PricePerNight
);
