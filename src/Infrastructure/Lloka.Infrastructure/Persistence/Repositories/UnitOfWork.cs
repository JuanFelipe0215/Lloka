using Lloka.Application.Common.Interfaces;

namespace Lloka.Infrastructure.Persistence.Repositories;

public class UnitOfWork(LlokaDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
