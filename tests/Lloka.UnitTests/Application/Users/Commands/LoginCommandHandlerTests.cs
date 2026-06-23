using FluentAssertions;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Application.Users.Commands.Login;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Moq;

namespace Lloka.UnitTests.Application.Users.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository>  _userRepo        = new();
    private readonly Mock<IPasswordHasher>  _passwordHasher  = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();

    private LoginCommandHandler CreateHandler() => new(
        _userRepo.Object,
        _passwordHasher.Object,
        _jwtTokenService.Object);

    private static User BuildApprovedUser()
    {
        var user = User.Create("user@test.com", "hash", "Ana", "López");
        user.UpdateKycStatus(KycStatus.Approved);
        return user;
    }

    // (a) El usuario no existe → UnauthorizedException con el mensaje genérico.
    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(
            new LoginCommand("noexiste@test.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("Credenciales inválidas.");
    }

    // (b) Contraseña incorrecta → UnauthorizedException con EXACTAMENTE el mismo mensaje que (a).
    //     Verifica que no se filtra si el email existe o no.
    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedException_WithSameMessage()
    {
        var user = BuildApprovedUser();
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(false);

        var act = () => CreateHandler().Handle(
            new LoginCommand(user.Email, "wrongpassword"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("Credenciales inválidas.");
    }

    // (c) Credenciales correctas → retorna token y datos del usuario.
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenAndUserData()
    {
        const string expectedToken = "header.payload.signature";
        var user = BuildApprovedUser();

        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(user.Id, user.Email, user.IsOwner))
                        .Returns(expectedToken);

        var result = await CreateHandler().Handle(
            new LoginCommand(user.Email, "correctpassword"),
            CancellationToken.None);

        result.Token.Should().Be(expectedToken);
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FullName.Should().Be(user.FullName);
    }
}
