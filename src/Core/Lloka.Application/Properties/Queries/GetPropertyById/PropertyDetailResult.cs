namespace Lloka.Application.Properties.Queries.GetPropertyById;

public record PropertyDetailResult(
    Guid                PropertyId,
    Guid                OwnerId,
    string              Title,
    string              Description,
    string              Street,
    string              City,
    string              Department,
    string              Country,
    decimal             Latitude,
    decimal             Longitude,
    string?             PostalCode,
    decimal             PricePerNight,
    int                 MaxGuests,
    string              Status,
    IReadOnlyList<string> ImageUrls
);
