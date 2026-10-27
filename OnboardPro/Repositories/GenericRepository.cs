using Microsoft.EntityFrameworkCore;
using OnboardPro.Data;

namespace OnboardPro.Repositories;

public sealed class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
{
    private readonly OnboardProDbContext _dbContext;
    private readonly DbSet<TEntity> _set;

    public GenericRepository(OnboardProDbContext dbContext)
    {
        _dbContext = dbContext;
        _set = dbContext.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _set.FindAsync([id], cancellationToken).AsTask();

    public Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => _dbContext.Entry(entity).State = EntityState.Modified;

    public void Remove(TEntity entity) => _set.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
