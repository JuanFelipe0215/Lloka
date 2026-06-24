namespace Lloka.Application.Users.Commands.RegisterUser;

public record RegisterUserResponse(Guid UserId, string Email, string FullName, bool IsOwner);
