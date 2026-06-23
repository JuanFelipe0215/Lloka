using FluentAssertions;
using Lloka.Application.Bookings.Commands.CreateBooking;

namespace Lloka.UnitTests.Application.Bookings.Commands;

public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _sut = new();

    private static CreateBookingCommand ValidCommand() => new(
        PropertyId:        Guid.NewGuid(),
        GuestId:           Guid.NewGuid(),
        CheckInDate:       new DateOnly(2025, 8, 10),
        CheckOutDate:      new DateOnly(2025, 8, 13),
        GuestCount:        2);

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyPropertyId_FailsOnPropertyId()
    {
        var cmd    = ValidCommand() with { PropertyId = Guid.Empty };
        var result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.PropertyId));
    }

    [Fact]
    public void Validate_EmptyGuestId_FailsOnGuestId()
    {
        var cmd    = ValidCommand() with { GuestId = Guid.Empty };
        var result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.GuestId));
    }

    [Fact]
    public void Validate_CheckOutEqualToCheckIn_FailsOnCheckOutDate()
    {
        var date   = new DateOnly(2025, 8, 10);
        var cmd    = ValidCommand() with { CheckInDate = date, CheckOutDate = date };
        var result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.CheckOutDate));
    }

    [Fact]
    public void Validate_CheckOutBeforeCheckIn_FailsOnCheckOutDate()
    {
        var cmd    = ValidCommand() with { CheckInDate = new DateOnly(2025, 8, 13), CheckOutDate = new DateOnly(2025, 8, 10) };
        var result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.CheckOutDate));
    }

    [Fact]
    public void Validate_ZeroGuestCount_FailsOnGuestCount()
    {
        var cmd    = ValidCommand() with { GuestCount = 0 };
        var result = _sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.GuestCount));
    }
}
