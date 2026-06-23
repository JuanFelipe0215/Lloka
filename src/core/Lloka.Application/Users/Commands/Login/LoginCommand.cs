using MediatR;

namespace Lloka.Application.Users.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
