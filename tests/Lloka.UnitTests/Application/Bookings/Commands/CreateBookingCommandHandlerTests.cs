using FluentAssertions;
using Lloka.Application.Bookings.Commands.CreateBooking;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Lloka.Domain.ValueObjects;
using Moq;

namespace Lloka.UnitTests.Application.Bookings.Commands;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IPropertyRepository>       _propertyRepo  = new();
    private readonly Mock<IUserRepository>           _userRepo      = new();
    private readonly Mock<IBookingRepository>        _bookingRepo   = new();
    private readonly Mock<IRepository<OutboxMessage>> _outboxRepo   = new();
    private readonly Mock<IUnitOfWork>               _unitOfWork    = new();

    private CreateBookingCommandHandler CreateHandler() => new(
        _propertyRepo.Object,
        _userRepo.Object,
        _bookingRepo.Object,
        _outboxRepo.Object,
        _unitOfWork.Object);

    // Builds a valid active Property with a known OwnerId
    private static Property BuildActiveProperty(Guid ownerId, int maxGuests = 4)
    {
        var address = new Address("Cra 7 #1-1", "Bogotá", "Cundinamarca", "Colombia", 4.71m, -74.07m);
        return Property.Create(ownerId, "Casa Test", "Descripción", address, 100_000m, maxGuests);
    }

    private static User BuildApprovedUser()
    {
        var user = User.Create("guest@test.com", "hash", "Juan", "Pérez");
        user.UpdateKycStatus(KycStatus.Approved);
        return user;
    }

    private static CreateBookingCommand ValidCommand(Guid propertyId, Guid guestId) => new(
        PropertyId:   propertyId,
        GuestId:      guestId,
        CheckInDate:  new DateOnly(2025, 9, 1),
        CheckOutDate: new DateOnly(2025, 9, 5),
        GuestCount:   2);

    // --- Happy path ---

    [Fact]
    public async Task Handle_ValidRequest_ReturnsResponseAndPersistsBookingAndOutbox()
    {
        var ownerId     = Guid.NewGuid();
        var guestId     = Guid.NewGuid();
        var property    = BuildActiveProperty(ownerId);
        var guest       = BuildApprovedUser();
        var handler     = CreateHandler();
        var command     = ValidCommand(property.Id, guestId);

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);
        _userRepo.Setup(r => r.GetByIdAsync(guestId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        _bookingRepo.Setup(r => r.HasConfirmedBookingAsync(guestId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false); // primera reserva — KYC Approved → pasa
        _bookingRepo.Setup(r => r.HasOverlapAsync(property.Id, It.IsAny<StayPeriod>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.BookingId.Should().NotBeEmpty();
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.Nights.Should().Be(4);
        result.TotalAmount.Should().Be(400_000m);

        _bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxRepo .Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork .Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Property no existe ---

    [Fact]
    public async Task Handle_PropertyNotFound_ThrowsNotFoundException()
    {
        var command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());
        _propertyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Property?)null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- Property inactiva ---

    [Fact]
    public async Task Handle_PropertyInactive_ThrowsConflictException()
    {
        var ownerId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId);
        property.Deactivate();
        var command  = ValidCommand(property.Id, Guid.NewGuid());

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // --- Owner reserva su propio inmueble ---

    [Fact]
    public async Task Handle_OwnerBookingOwnProperty_ThrowsConflictException()
    {
        var ownerId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId);
        var command  = ValidCommand(property.Id, ownerId); // GuestId == OwnerId

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*propietario*");
    }

    // --- Primera reserva sin KYC aprobado (Pending) ---

    [Fact]
    public async Task Handle_FirstBookingKycPending_ThrowsConflictException()
    {
        var ownerId  = Guid.NewGuid();
        var guestId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId);
        var guest    = User.Create("g@test.com", "hash", "Ana", "López"); // KycStatus = NotSubmitted/Pending

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);
        _userRepo.Setup(r => r.GetByIdAsync(guestId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        _bookingRepo.Setup(r => r.HasConfirmedBookingAsync(guestId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var command = ValidCommand(property.Id, guestId);
        var act     = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*KYC*");
    }

    // --- Primera reserva KYC Rejected ---

    [Fact]
    public async Task Handle_FirstBookingKycRejected_ThrowsConflictException()
    {
        var ownerId  = Guid.NewGuid();
        var guestId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId);
        var guest    = User.Create("g@test.com", "hash", "Ana", "López");
        guest.UpdateKycStatus(KycStatus.Rejected);

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);
        _userRepo.Setup(r => r.GetByIdAsync(guestId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        _bookingRepo.Setup(r => r.HasConfirmedBookingAsync(guestId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var command = ValidCommand(property.Id, guestId);
        var act     = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*KYC*");
    }

    // --- No es primera reserva — KYC no se valida ---

    [Fact]
    public async Task Handle_NotFirstBooking_KycNotApproved_Succeeds()
    {
        var ownerId  = Guid.NewGuid();
        var guestId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId);
        var guest    = User.Create("g@test.com", "hash", "Ana", "López"); // KYC Pending

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);
        _userRepo.Setup(r => r.GetByIdAsync(guestId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        _bookingRepo.Setup(r => r.HasConfirmedBookingAsync(guestId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true); // ya tiene reservas → omitir KYC
        _bookingRepo.Setup(r => r.HasOverlapAsync(property.Id, It.IsAny<StayPeriod>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var result = await CreateHandler().Handle(ValidCommand(property.Id, guestId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed);
    }

    // --- GuestCount excede MaxGuests ---

    [Fact]
    public async Task Handle_GuestCountExceedsMaxGuests_ThrowsConflictException()
    {
        var ownerId  = Guid.NewGuid();
        var guestId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId, maxGuests: 3);
        var guest    = BuildApprovedUser();
        var command  = ValidCommand(property.Id, guestId) with { GuestCount = 10 };

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);
        _userRepo.Setup(r => r.GetByIdAsync(guestId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        _bookingRepo.Setup(r => r.HasConfirmedBookingAsync(guestId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*máximo*3*");
    }

    // --- Solapamiento de fechas ---

    [Fact]
    public async Task Handle_DateOverlap_ThrowsConflictException()
    {
        var ownerId  = Guid.NewGuid();
        var guestId  = Guid.NewGuid();
        var property = BuildActiveProperty(ownerId);
        var guest    = BuildApprovedUser();

        _propertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(property);
        _userRepo.Setup(r => r.GetByIdAsync(guestId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        _bookingRepo.Setup(r => r.HasConfirmedBookingAsync(guestId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
        _bookingRepo.Setup(r => r.HasOverlapAsync(property.Id, It.IsAny<StayPeriod>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        var act = () => CreateHandler().Handle(ValidCommand(property.Id, guestId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*disponible*");
    }
}
