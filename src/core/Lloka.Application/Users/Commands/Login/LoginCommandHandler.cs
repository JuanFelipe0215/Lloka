using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using MediatR;

namespace Lloka.Application.Users.Commands.Login;

public class LoginCommandHandler(
    IUserRepository  userRepo,
    IPasswordHasher  passwordHasher,
    IJwtTokenService jwtTokenService
) : IRequestHandler<LoginCommand, LoginResponse>
{
    private const string InvalidCredentials = "Credenciales inválidas.";

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException(InvalidCredentials);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials);

        var token = jwtTokenService.GenerateToken(user.Id, user.Email, user.IsOwner);

        return new LoginResponse(token, user.Id, user.Email, user.FullName);
    }
}
