namespace Lloka.Application.Properties.Queries.SearchProperties;

public record SearchPropertyResult(
    Guid    PropertyId,
    string  Title,
    string  City,
    string  Department,
    decimal PricePerNight,
    int     MaxGuests,
    string? ThumbnailUrl
);
