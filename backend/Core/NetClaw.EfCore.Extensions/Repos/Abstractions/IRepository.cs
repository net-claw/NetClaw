using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace NetClaw.EfCore.Extensions.Repos.Abstractions;

public interface IRepository<TEntity> where TEntity : class
{
    ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    void Delete(TEntity entity);

    void DeleteRange(IEnumerable<TEntity> entities);

    EntityEntry<TEntity> Entry(TEntity entity);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(object keyValue, CancellationToken cancellationToken = default);

    ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);

    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    IQueryable<TEntity> Query();

    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> filter);

    IQueryable<TEntity> QueryNoTracking();

    IQueryable<TModel> Query<TModel>(Expression<Func<TEntity, bool>> filter) where TModel : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}
