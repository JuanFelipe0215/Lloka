using FluentAssertions;
using Lloka.Application.Bookings.Commands.CancelBooking;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Lloka.Domain.ValueObjects;
using Moq;

namespace Lloka.UnitTests.Application.Bookings.Commands;

public class CancelBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository>          _bookingRepo = new();
    private readonly Mock<IRepository<OutboxMessage>>  _outboxRepo  = new();
    private readonly Mock<IUnitOfWork>                 _unitOfWork  = new();

    private CancelBookingCommandHandler CreateHandler() => new(
        _bookingRepo.Object,
        _outboxRepo.Object,
        _unitOfWork.Object);

    private static Booking BuildConfirmedBooking(Guid guestId)
    {
        var address  = new Address("Cra 7 #1-1", "Bogotá", "Cundinamarca", "Colombia", 4.71m, -74.07m);
        var property = Property.Create(Guid.NewGuid(), "Casa Test", "Desc", address, 100_000m, 4);
        var booking  = Booking.Create(property.Id, guestId, new StayPeriod(new DateOnly(2025, 10, 1), new DateOnly(2025, 10, 5)), 100_000m, 2);
        booking.Confirm();
        return booking;
    }

    [Fact]
    public async Task Handle_HappyPath_CancelsBookingAndPersistsOutbox()
    {
        var guestId = Guid.NewGuid();
        var booking = BuildConfirmedBooking(guestId);
        var command = new CancelBookingCommand(booking.Id, guestId);

        _bookingRepo.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(booking);

        await CreateHandler().Handle(command, CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        _bookingRepo.Verify(r => r.Update(booking), Times.Once);
        _outboxRepo .Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork .Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsNotFoundException()
    {
        var command = new CancelBookingCommand(Guid.NewGuid(), Guid.NewGuid());
        _bookingRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Booking?)null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RequesterIsNotGuest_ThrowsConflictException()
    {
        var guestId     = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var booking     = BuildConfirmedBooking(guestId);
        var command     = new CancelBookingCommand(booking.Id, otherUserId);

        _bookingRepo.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(booking);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*permisos*");
    }
}
