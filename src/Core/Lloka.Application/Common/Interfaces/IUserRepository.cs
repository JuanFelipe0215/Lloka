using Lloka.Domain.Entities;

namespace Lloka.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetByIdWithKycAsync(Guid userId, CancellationToken ct = default);
}
