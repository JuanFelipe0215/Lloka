using MediatR;

namespace Lloka.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool   IsOwner = false
) : IRequest<RegisterUserResponse>;
