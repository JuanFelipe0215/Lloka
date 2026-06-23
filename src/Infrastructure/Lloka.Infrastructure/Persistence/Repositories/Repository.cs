using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Common;

namespace Lloka.Infrastructure.Persistence.Repositories;

public class Repository<T>(LlokaDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly LlokaDbContext Context = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Context.Set<T>().FindAsync([id], ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Context.Set<T>().AddAsync(entity, ct);

    public void Update(T entity) => Context.Set<T>().Update(entity);

    public void Remove(T entity) => Context.Set<T>().Remove(entity);
}
