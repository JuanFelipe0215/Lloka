using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lloka.Infrastructure.Persistence.Repositories;

public class UserRepository(LlokaDbContext context)
    : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
}
