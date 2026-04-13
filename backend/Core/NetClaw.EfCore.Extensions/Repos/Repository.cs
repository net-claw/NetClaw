using System.Linq.Expressions;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NetClaw.EfCore.Extensions.Repos.Abstractions;

namespace NetClaw.EfCore.Extensions.Repos;

public class Repository<TEntity>(
    DbContext dbContext, 
    IServiceProvider? provider = null) : IRepository<TEntity>
    where TEntity : class
{

    private readonly IMapper? _mapper = provider?.GetService(typeof(IMapper)) as IMapper;

    #region Methods

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default) =>
        Query(filter).CountAsync(cancellationToken);

    public virtual async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await dbContext.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public virtual async ValueTask AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) =>
        await dbContext.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => dbContext.Database.BeginTransactionAsync(cancellationToken);

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
        => Query(filter).AnyAsync(cancellationToken);

    public virtual void Delete(TEntity entity) => dbContext.Set<TEntity>().Remove(entity);

    public virtual void DeleteRange(IEnumerable<TEntity> entities) => dbContext.Set<TEntity>().RemoveRange(entities);

    public EntityEntry<TEntity> Entry(TEntity entity) => dbContext.Entry(entity);

    public ValueTask<TEntity?> FindAsync(object keyValue, CancellationToken cancellationToken = default)
        => FindAsync([keyValue], cancellationToken);

    public async ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default)
        => await dbContext.FindAsync<TEntity>(keyValues, cancellationToken);

    public Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default) =>
        Query(filter).FirstOrDefaultAsync(cancellationToken);

    public IQueryable<TModel> Query<TModel>(Expression<Func<TEntity, bool>> filter)
        where TModel : class
    {
        if (_mapper is null) throw new InvalidOperationException("IMapper is not registered.");

        var query = Query(filter);
        return query.ProjectToType<TModel>(_mapper.Config);
    }

    public IQueryable<TEntity> Query() => dbContext.Set<TEntity>();

    public IQueryable<TEntity> QueryNoTracking() => Query().AsNoTracking();

    public IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> filter) => Query().Where(filter);

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.AddNewEntitiesFromNavigations(cancellationToken).ConfigureAwait(false);
        var handler = provider?.GetKeyedService<IEfCoreConcurrencyHandler>(dbContext.GetType().FullName);
        return await dbContext.SaveChangesWithConcurrencyHandlingAsync(handler, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        dbContext.Entry(entity).State = EntityState.Modified;

        var newEntities = dbContext.GetNewEntitiesFromNavigations(dbContext.Entry(entity)).ToList();
        await dbContext.AddRangeAsync(newEntities, cancellationToken).ConfigureAwait(false);
        return newEntities.Count;
    }

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities) await UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
