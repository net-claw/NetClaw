using Microsoft.EntityFrameworkCore;

namespace NetClaw.EfCore.Extensions;


/// <summary>
///     Provides extension methods for <see cref="DbContext" /> that wrap
///     <see cref="DbContext.SaveChangesAsync(CancellationToken)" /> with automatic
///     optimistic-concurrency conflict detection and configurable resolution strategies.
/// </summary>
public static class EfCoreSaveChangesExtension
{
    #region Methods

    /// <summary>
    ///     Saves all pending changes in the <see cref="DbContext" />, automatically handling any
    ///     <see cref="DbUpdateConcurrencyException" /> according to the strategy returned by <paramref name="handler" />.
    /// </summary>
    /// <param name="dbContext">The <see cref="DbContext" /> whose changes should be saved.</param>
    /// <param name="handler">
    ///     An optional <see cref="IEfCoreConcurrencyHandler" /> that decides how to resolve concurrency conflicts.
    ///     If <c>null</c>, a default <see cref="EfCoreConcurrencyHandler" /> is used, which applies
    ///     <see cref="EfConcurrencyResolution.DatabaseWins" /> semantics.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    ///     The number of state entries written to the database, or <c>0</c> if the conflict was resolved
    ///     with <see cref="EfConcurrencyResolution.IgnoreChanges" /> or the retry limit was exceeded.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The following resolution strategies are supported:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <term><see cref="EfConcurrencyResolution.DatabaseWins" /></term>
    ///             <description>
    ///                 Overwrites the client's pending changes with the latest database values,
    ///                 then retries the save. The database state takes precedence.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term><see cref="EfConcurrencyResolution.ClientWins" /></term>
    ///             <description>
    ///                 Refreshes the original values from the database while keeping the client's
    ///                 current changes, then retries the save. The client state takes precedence.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term><see cref="EfConcurrencyResolution.RetrySaveChanges" /></term>
    ///             <description>
    ///                 Retries the save without applying automatic value resolution. Use when the
    ///                 handler has already manually merged the conflicting entries.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term><see cref="EfConcurrencyResolution.IgnoreChanges" /></term>
    ///             <description>Discards the client's changes and returns <c>0</c>.</description>
    ///         </item>
    ///         <item>
    ///             <term><see cref="EfConcurrencyResolution.RethrowException" /></term>
    ///             <description>Re-throws the <see cref="DbUpdateConcurrencyException" /> to the caller.</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Retries are capped by <see cref="IEfCoreConcurrencyHandler.MaxRetryCount" />.
    ///         Once exceeded, the method returns <c>0</c> without throwing.
    ///     </para>
    /// </remarks>
    /// <exception cref="DbUpdateConcurrencyException">
    ///     Thrown when the handler returns <see cref="EfConcurrencyResolution.RethrowException" />.
    /// </exception>
    public static async Task<int> SaveChangesWithConcurrencyHandlingAsync(
        this DbContext dbContext,
        IEfCoreConcurrencyHandler? handler = null,
        CancellationToken cancellationToken = default)
    {
        handler ??= new EfCoreConcurrencyHandler();
        var retryCount = 0;

        while (true)
            try
            {
                return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var resolution = await handler.HandlingAsync(dbContext, ex, cancellationToken).ConfigureAwait(false);

                switch (resolution)
                {
                    case EfConcurrencyResolution.DatabaseWins:
                        await ApplyDatabaseWinsAsync(ex, cancellationToken).ConfigureAwait(false);
                        break;

                    case EfConcurrencyResolution.ClientWins:
                        await ApplyClientWinsAsync(ex, cancellationToken).ConfigureAwait(false);
                        break;

                    case EfConcurrencyResolution.RetrySaveChanges:
                        break;

                    case EfConcurrencyResolution.IgnoreChanges:
                        return 0;

                    case EfConcurrencyResolution.RethrowException:
                        throw;
                }

                retryCount++;
                if (retryCount > handler.MaxRetryCount) return 0;
            }
    }

    /// <summary>
    ///     Applies Database Wins resolution: overwrites the client's current and original values
    ///     with the latest values from the database for each conflicting entry.
    ///     If the entity no longer exists in the database, it is detached from the context.
    /// </summary>
    private static async Task ApplyDatabaseWinsAsync(
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        foreach (var entry in exception.Entries)
        {
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
            if (databaseValues == null)
            {
                entry.State = EntityState.Detached;
                continue;
            }

            entry.OriginalValues.SetValues(databaseValues);
            entry.CurrentValues.SetValues(databaseValues);
        }
    }

    /// <summary>
    ///     Applies Client Wins resolution: refreshes the original values from the database
    ///     while preserving the client's current changes for each conflicting entry.
    ///     If the entity no longer exists in the database, the entry is left unchanged so
    ///     the retry will insert it as a new row.
    /// </summary>
    private static async Task ApplyClientWinsAsync(
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        foreach (var entry in exception.Entries)
        {
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
            if (databaseValues == null) continue;

            var clientValues = entry.CurrentValues.Clone();
            entry.OriginalValues.SetValues(databaseValues);
            entry.CurrentValues.SetValues(clientValues);
        }
    }

    #endregion
}
