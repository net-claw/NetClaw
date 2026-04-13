using Microsoft.EntityFrameworkCore;

namespace NetClaw.EfCore.Extensions;


/// <summary>
///     Represents the resolution strategy to apply when an EF Core concurrency conflict is detected.
/// </summary>
public enum EfConcurrencyResolution
{
    /// <summary>
    ///     Discards the client's pending changes and keeps the current database values unchanged.
    ///     The save operation returns <c>0</c> without retrying.
    /// </summary>
    IgnoreChanges,

    /// <summary>
    ///     Overwrites the client's pending changes with the latest database values.
    ///     The database state takes precedence. The save operation will be retried.
    /// </summary>
    DatabaseWins,

    /// <summary>
    ///     Overwrites the original values with the latest database values while preserving
    ///     the client's current changes. The client state takes precedence. The save operation will be retried.
    /// </summary>
    ClientWins,

    /// <summary>
    ///     Retries the save operation without applying any automatic conflict resolution.
    ///     Use this when the handler has already manually resolved the conflict before returning.
    /// </summary>
    RetrySaveChanges,

    /// <summary>
    ///     Re-throws the <see cref="DbUpdateConcurrencyException" /> to the caller for manual handling.
    /// </summary>
    RethrowException
}

/// <summary>
///     Defines a contract for handling <see cref="DbUpdateConcurrencyException" /> raised by EF Core
///     and determining the resolution strategy to apply.
/// </summary>
public interface IEfCoreConcurrencyHandler
{
    /// <summary>
    ///     Gets the maximum number of retry attempts before the save operation gives up and returns <c>0</c>.
    ///     Defaults to <c>3</c>.
    /// </summary>
    public int MaxRetryCount => 3;

    /// <summary>
    ///     Inspects the given <see cref="DbUpdateConcurrencyException" /> and returns the
    ///     <see cref="EfConcurrencyResolution" /> strategy that should be applied.
    /// </summary>
    /// <param name="context">The <see cref="DbContext" /> instance in which the conflict occurred.</param>
    /// <param name="exception">The concurrency exception containing the conflicting entries.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    ///     A <see cref="Task{EfConcurrencyResolution}" /> representing the chosen resolution strategy.
    /// </returns>
    Task<EfConcurrencyResolution> HandlingAsync(DbContext context, DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Default implementation of <see cref="IEfCoreConcurrencyHandler" /> that applies a
///     <b>Database Wins</b> strategy: on conflict, the client's pending changes are overwritten
///     with the latest values from the database and the save is retried.
/// </summary>
/// <remarks>
///     Database Wins is the safe default — it prevents silent data loss by ensuring that
///     the most recently committed database state is never silently overwritten by a stale client write.
///     Use <see cref="EfConcurrencyResolution.ClientWins" /> (via a custom handler) only when
///     last-write-wins semantics are explicitly required by the domain.
/// </remarks>
public sealed class EfCoreConcurrencyHandler : IEfCoreConcurrencyHandler
{
    #region Methods

    /// <inheritdoc />
    public Task<EfConcurrencyResolution> HandlingAsync(DbContext context, DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EfConcurrencyResolution.DatabaseWins);

    #endregion
}
