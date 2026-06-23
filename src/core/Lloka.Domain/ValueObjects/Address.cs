using Ardalis.GuardClauses;
using Lloka.Domain.Common;

namespace Lloka.Domain.ValueObjects;

public sealed record Address
{
    public string  Street     { get; }
    public string  City       { get; }
    public string  Department { get; }  
    public string  Country    { get; }
    public decimal Latitude   { get; }  // -90 a 90
    public decimal Longitude  { get; }  // -180 a 180
    public string? PostalCode { get; }

    public Address(
        string street,
        string city,
        string department,
        string country,
        decimal latitude,
        decimal longitude,
        string? postalCode = null)
    {
        Street     = Guard.Against.NullOrWhiteSpace(street,     nameof(street));
        City       = Guard.Against.NullOrWhiteSpace(city,       nameof(city));
        Department = Guard.Against.NullOrWhiteSpace(department, nameof(department));
        Country    = Guard.Against.NullOrWhiteSpace(country,    nameof(country));

        if (latitude < -90m || latitude > 90m)
            throw new DomainException($"Latitude debe estar entre -90 y 90. Valor recibido: {latitude}.");
        if (longitude < -180m || longitude > 180m)
            throw new DomainException($"Longitude debe estar entre -180 y 180. Valor recibido: {longitude}.");

        Latitude   = latitude;
        Longitude  = longitude;
        PostalCode = postalCode;
    }
}
