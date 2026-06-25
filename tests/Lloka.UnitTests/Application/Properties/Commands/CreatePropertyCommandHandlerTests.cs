using FluentAssertions;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Application.Properties.Commands.CreateProperty;
using Lloka.Domain.Common;
using Lloka.Domain.Entities;
using Moq;

namespace Lloka.UnitTests.Application.Properties.Commands;

public class CreatePropertyCommandHandlerTests
{
    private readonly Mock<IUserRepository>     _userRepo     = new();
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IUnitOfWork>         _unitOfWork   = new();

    private CreatePropertyCommandHandler CreateHandler() => new(
        _userRepo.Object,
        _propertyRepo.Object,
        _unitOfWork.Object);

    private static CreatePropertyCommand ValidCommand(Guid ownerId) => new(
        OwnerId:      ownerId,
        Title:        "Apartamento en Bogotá",
        Description:  "Hermoso apto en el centro",
        Street:       "Cra 7 #1-1",
        City:         "Bogotá",
        Department:   "Cundinamarca",
        Country:      "Colombia",
        Latitude:     4.71m,
        Longitude:    -74.07m,
        PostalCode:   null,
        PricePerNight: 150_000m,
        MaxGuests:    4);

    private static User BuildOwner()
    {
        var user = User.Create("owner@test.com", "hash", "Carlos", "García");
        user.BecomeOwner();
        return user;
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesPropertyAndSaves()
    {
        var owner   = BuildOwner();
        var command = ValidCommand(owner.Id);

        _userRepo.Setup(r => r.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(owner);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PropertyId.Should().NotBeEmpty();
        result.Title.Should().Be(command.Title);
        _propertyRepo.Verify(r => r.AddAsync(It.IsAny<Property>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork  .Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserIsNotOwner_ThrowsConflictException()
    {
        // User.Create() deja IsOwner = false por defecto
        var guest   = User.Create("guest@test.com", "hash", "María", "López");
        var command = ValidCommand(guest.Id);

        _userRepo.Setup(r => r.GetByIdAsync(guest.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*propietarios*");
    }

    [Fact]
    public async Task Handle_InvalidCoordinates_ThrowsDomainException()
    {
        var owner   = BuildOwner();
        // Latitud 999 está fuera del rango válido (-90 a 90)
        var command = ValidCommand(owner.Id) with { Latitude = 999m };

        _userRepo.Setup(r => r.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(owner);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        // Address constructor lanza DomainException antes de crear la Property.
        // En producción el ValidationBehavior del pipeline atrapa esto primero si
        // el Validator valida coordenadas; este test documenta la segunda capa de defensa.
        await act.Should().ThrowAsync<DomainException>();
    }
}
