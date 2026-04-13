using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NetClaw.EfCore.Extensions;


/// <summary>
///     Provides helper extensions for reading EF Core collection navigations and locating related entities
///     that should be tracked as part of the current <see cref="DbContext" /> graph.
/// </summary>
public static class EfCoreNavigationExtensions
{
    #region Methods

    /// <summary>
    ///     Reads the values currently exposed by a collection navigation on the specified entity instance.
    /// </summary>
    /// <param name="obj">The entity instance that owns the collection navigation.</param>
    /// <param name="navigation">The EF Core navigation metadata describing the collection member to read.</param>
    /// <returns>
    ///     The objects currently available through the collection navigation, or an empty sequence when the
    ///     navigation cannot be read.
    /// </returns>
    public static IEnumerable<object> GetNavigations(this object obj, INavigation navigation)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(navigation);

        if (navigation.PropertyInfo is not null)
            return navigation.PropertyInfo.GetValue(obj) as IEnumerable<object> ?? [];

        if (navigation.FieldInfo is not null)
            return navigation.FieldInfo.GetValue(obj) as IEnumerable<object> ?? [];

        return [];
    }

    #endregion

    /// <summary>
    ///     Determines whether the specified <see cref="EntityEntry" /> should be treated as a new entity
    ///     that still needs to be added to the current context.
    /// </summary>
    /// <param name="entry">The tracked entity entry to evaluate.</param>
    /// <returns>
    ///     <c>true</c> when the entry does not have a key yet, is already marked as
    ///     <see cref="EntityState.Added" />, or has no original key values recorded; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNewEntity(this EntityEntry entry)
    {
        // If the entity's key is not set, it is considered new.
        if (!entry.IsKeySet) return true;

        if (entry.State is EntityState.Added) return true;

        // If the entity is not in the Detached state, it is not new.
        if (entry.State is EntityState.Modified or EntityState.Deleted) return false;

        var keyValues = entry.GetOriginalKeyValues().ToList();
        return keyValues.Count <= 0 || keyValues.TrueForAll(kv => kv is null);
    }

    /// <summary>
    ///     Returns the original primary key values currently known for the specified entry.
    /// </summary>
    /// <param name="entry">The tracked entry whose original primary key values should be read.</param>
    /// <returns>
    ///     A sequence containing the original values of each primary key property, or an empty sequence when
    ///     the entity type does not define a primary key.
    /// </returns>
    public static IEnumerable<object?> GetOriginalKeyValues(this EntityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Retrieve the primary key metadata for the entity.
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey is null) return [];

        // Get the original values for each primary key property.
        return primaryKey.Properties.Select(p => entry.OriginalValues[p]);
    }


    /// <summary>
    ///     Finds new child entities reachable through collection navigations and adds them to the context
    ///     in a single asynchronous batch.
    /// </summary>
    /// <param name="context">The <see cref="DbContext" /> whose tracked entity graph should be scanned.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous add operation.</param>
    /// <returns>The number of newly discovered entities that were added to the context.</returns>
    public static async Task<int> AddNewEntitiesFromNavigations<TDbContext>(
        this TDbContext context,
        CancellationToken cancellationToken = default
    ) where TDbContext : DbContext
    {
        var newEntities = context.GetNewEntitiesFromNavigations().ToList();
        await context.AddRangeAsync(newEntities, cancellationToken);
        return newEntities.Count;
    }

    /// <summary>
    ///     Returns tracked entries that may act as roots for navigation scanning after forcing change detection.
    /// </summary>
    /// <param name="context">The <see cref="DbContext" /> whose tracked entries should be inspected.</param>
    /// <returns>
    ///     The tracked entries whose state is <see cref="EntityState.Detached" />,
    ///     <see cref="EntityState.Modified" />, or <see cref="EntityState.Unchanged" />.
    /// </returns>
    public static IEnumerable<EntityEntry> GetPossibleUpdatingEntities<TDbContext>(this TDbContext context)
        where TDbContext : DbContext
    {
        if (context.ChangeTracker is null) return [];
        context.ChangeTracker.DetectChanges();
        return context.ChangeTracker.Entries().Where(e =>
            e.State is EntityState.Detached or EntityState.Modified or EntityState.Unchanged);
    }

    /// <summary>
    ///     Scans the collection navigations of a specific tracked root entry and yields child entities
    ///     that appear to be new to the current context.
    /// </summary>
    /// <param name="context">The <see cref="DbContext" /> that owns the tracked entity graph.</param>
    /// <param name="entity">The root entity entry whose collection navigations should be scanned.</param>
    /// <returns>
    ///     A sequence of child entities discovered under collection navigations that satisfy
    ///     <see cref="IsNewEntity(EntityEntry)" />.
    /// </returns>
    public static IEnumerable<object> GetNewEntitiesFromNavigations<TDbContext>(this TDbContext context, EntityEntry entity)
        where TDbContext : DbContext
    {
        var navigations = context.GetCollectionNavigations(entity.Metadata.ClrType);
        foreach (var nav in navigations)
        foreach (var i in entity.Entity.GetNavigations(nav))
        {
            var item = context.Entry(i);
            if (item.IsNewEntity()) yield return i;
        }
    }

    /// <summary>
    ///     Scans all eligible tracked root entries in the context and yields child entities discovered as new
    ///     through collection navigations.
    /// </summary>
    /// <param name="context">The <see cref="DbContext" /> whose tracked graph should be scanned.</param>
    /// <returns>A sequence of new entities discovered from collection navigations across the tracked graph.</returns>
    public static IEnumerable<object> GetNewEntitiesFromNavigations<TDbContext>(this TDbContext context)
        where TDbContext : DbContext
    {
        var roots = context.GetPossibleUpdatingEntities();
        foreach (var root in roots)
        {
            var list = context.GetNewEntitiesFromNavigations(root);
            foreach (var o in list) yield return o;
        }
    }

    /// <summary>
    ///     Returns collection navigation metadata for the specified CLR entity type from the current EF model.
    /// </summary>
    /// <param name="context">The <see cref="DbContext" /> whose model should be queried.</param>
    /// <param name="entityType">The CLR entity type whose collection navigations should be returned.</param>
    /// <returns>
    ///     A sequence of <see cref="INavigation" /> metadata entries representing non-shadow collection
    ///     navigations defined for the supplied entity type.
    /// </returns>
    public static IEnumerable<INavigation> GetCollectionNavigations<TDbContext>(this TDbContext context, Type entityType)
        where TDbContext : DbContext
    {
        var type = context.Model.FindEntityType(entityType);
        if (type is null) return [];

        return type
            .GetNavigations().Where(n => n.IsCollection && !n.IsShadowProperty());
    }
}
