namespace Lloka.Application.Common.Interfaces;

public interface IJwtTokenService
{
    // isOwner se incluye ahora para evitar un cambio de formato de token
    // cuando lleguen CreateProperty y el dashboard de Owner.
    string GenerateToken(Guid userId, string email, bool isOwner);
}
