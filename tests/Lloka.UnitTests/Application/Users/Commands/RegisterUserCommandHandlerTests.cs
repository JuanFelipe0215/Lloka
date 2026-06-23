using FluentAssertions;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Application.Users.Commands.RegisterUser;
using Lloka.Domain.Entities;
using Moq;

namespace Lloka.UnitTests.Application.Users.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo       = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork>     _unitOfWork     = new();

    private RegisterUserCommandHandler CreateHandler() => new(
        _userRepo.Object,
        _passwordHasher.Object,
        _unitOfWork.Object);

    private static RegisterUserCommand ValidCommand() => new(
        Email:     "nuevo@test.com",
        Password:  "password123",
        FirstName: "Juan",
        LastName:  "Pérez");

    // Email duplicado → ConflictException.
    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        var existing = User.Create("nuevo@test.com", "hash", "Otro", "Usuario");
        _userRepo.Setup(r => r.GetByEmailAsync("nuevo@test.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // Email nuevo → crea el usuario, llama AddAsync y SaveChangesAsync, retorna response correcto.
    [Fact]
    public async Task Handle_NewEmail_PersistsUserAndReturnsResponse()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>()))
                       .Returns("hashed_password");

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Email.Should().Be("nuevo@test.com");
        result.FullName.Should().Be("Juan Pérez");
        result.UserId.Should().NotBeEmpty();

        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
