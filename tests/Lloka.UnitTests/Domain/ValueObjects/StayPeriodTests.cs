using FluentAssertions;
using Lloka.Domain.Common;
using Lloka.Domain.ValueObjects;

namespace Lloka.UnitTests.Domain.ValueObjects;

public class StayPeriodTests
{
    // --- Happy path ---

    [Fact]
    public void Constructor_ValidRange_CreatesStayPeriod()
    {
        var checkIn  = new DateOnly(2025, 7, 10);
        var checkOut = new DateOnly(2025, 7, 13);

        var sut = new StayPeriod(checkIn, checkOut);

        sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ValidRange_ComputesNightsCorrectly()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));

        sut.Nights.Should().Be(3);
    }

    [Fact]
    public void Constructor_SingleNight_NightsIsOne()
    {
        var sut = new StayPeriod(new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 2));

        sut.Nights.Should().Be(1);
    }

    // --- Tiempos canónicos fijos (hora Colombia UTC-5) ---

    [Fact]
    public void Constructor_ValidRange_CheckInTimeIsFixed_14h_Colombia()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));

        sut.CheckIn.Hour.Should().Be(14);
        sut.CheckIn.Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    [Fact]
    public void Constructor_ValidRange_CheckOutTimeIsFixed_12h_Colombia()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));

        sut.CheckOut.Hour.Should().Be(12);
        sut.CheckOut.Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    // --- Fechas preservadas ---

    [Fact]
    public void Constructor_ValidRange_CheckInDatePreserved()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));

        sut.CheckIn.Year.Should().Be(2025);
        sut.CheckIn.Month.Should().Be(7);
        sut.CheckIn.Day.Should().Be(10);
    }

    [Fact]
    public void Constructor_ValidRange_CheckOutDatePreserved()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));

        sut.CheckOut.Year.Should().Be(2025);
        sut.CheckOut.Month.Should().Be(7);
        sut.CheckOut.Day.Should().Be(13);
    }

    // --- Invariantes de negocio ---

    [Fact]
    public void Constructor_SameDates_ThrowsDomainException()
    {
        var date = new DateOnly(2025, 7, 10);

        var act = () => new StayPeriod(date, date);

        act.Should().Throw<DomainException>()
           .WithMessage("*check-out*");
    }

    [Fact]
    public void Constructor_CheckOutBeforeCheckIn_ThrowsDomainException()
    {
        var act = () => new StayPeriod(new DateOnly(2025, 7, 13), new DateOnly(2025, 7, 10));

        act.Should().Throw<DomainException>();
    }

    // --- CalculateTotalAmount ---

    [Fact]
    public void CalculateTotalAmount_ReturnsCorrectProduct()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13)); // 3 noches

        var total = sut.CalculateTotalAmount(150_000m);

        total.Should().Be(450_000m);
    }

    [Fact]
    public void CalculateTotalAmount_ZeroPrice_ReturnsZero()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 12));

        sut.CalculateTotalAmount(0m).Should().Be(0m);
    }

    [Fact]
    public void CalculateTotalAmount_NegativePrice_ThrowsDomainException()
    {
        var sut = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 12));

        var act = () => sut.CalculateTotalAmount(-100m);

        act.Should().Throw<DomainException>();
    }

    // --- Igualdad estructural (sealed record) ---

    [Fact]
    public void TwoStayPeriods_WithSameDates_AreEqual()
    {
        var a = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));
        var b = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));

        a.Should().Be(b);
    }

    [Fact]
    public void TwoStayPeriods_WithDifferentDates_AreNotEqual()
    {
        var a = new StayPeriod(new DateOnly(2025, 7, 10), new DateOnly(2025, 7, 13));
        var b = new StayPeriod(new DateOnly(2025, 7, 11), new DateOnly(2025, 7, 14));

        a.Should().NotBe(b);
    }
}
