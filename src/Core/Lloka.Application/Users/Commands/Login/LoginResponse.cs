namespace Lloka.Application.Users.Commands.Login;

public record LoginResponse(string Token, Guid UserId, string Email, string FullName);
