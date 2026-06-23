using FluentAssertions;
using Lloka.Domain.Common;
using Lloka.Domain.ValueObjects;

namespace Lloka.UnitTests.Domain.ValueObjects;

public class AddressTests
{
    // Coordenadas de Bogotá para el happy path
    private const decimal BogotaLat = 4.710989m;
    private const decimal BogotaLon = -74.072092m;

    // --- Happy path ---

    [Fact]
    public void Constructor_ValidCoordinates_CreatesAddress()
    {
        var sut = new Address("Cra 7 #45-12", "Bogotá", "Cundinamarca", "Colombia",
                              BogotaLat, BogotaLon);

        sut.Latitude .Should().Be(BogotaLat);
        sut.Longitude.Should().Be(BogotaLon);
    }

    // --- Límites exactos válidos ---

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Constructor_LatitudeAtExactBoundary_DoesNotThrow(decimal lat)
    {
        var act = () => new Address("St", "City", "Dept", "Col", lat, 0m);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(180)]
    public void Constructor_LongitudeAtExactBoundary_DoesNotThrow(decimal lon)
    {
        var act = () => new Address("St", "City", "Dept", "Col", 0m, lon);

        act.Should().NotThrow();
    }

    // --- Latitude fuera de rango ---

    [Fact]
    public void Constructor_LatitudeTooLow_ThrowsDomainException()
    {
        var act = () => new Address("St", "City", "Dept", "Col", -90.001m, BogotaLon);

        act.Should().Throw<DomainException>().WithMessage("*Latitude*");
    }

    [Fact]
    public void Constructor_LatitudeTooHigh_ThrowsDomainException()
    {
        var act = () => new Address("St", "City", "Dept", "Col", 90.001m, BogotaLon);

        act.Should().Throw<DomainException>().WithMessage("*Latitude*");
    }

    // --- Longitude fuera de rango ---

    [Fact]
    public void Constructor_LongitudeTooLow_ThrowsDomainException()
    {
        var act = () => new Address("St", "City", "Dept", "Col", BogotaLat, -180.001m);

        act.Should().Throw<DomainException>().WithMessage("*Longitude*");
    }

    [Fact]
    public void Constructor_LongitudeTooHigh_ThrowsDomainException()
    {
        var act = () => new Address("St", "City", "Dept", "Col", BogotaLat, 180.001m);

        act.Should().Throw<DomainException>().WithMessage("*Longitude*");
    }
}
