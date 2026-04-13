namespace NetClaw.EfCore.Extensions.Entities;

/// <summary>
///     Defines the contract for an auditable entity with a specified key type.
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
/// <remarks>
///     This contract combines entity identity with creation and modification metadata.
/// </remarks>
public interface IAuditedEntity<out TKey> : IEntity<TKey>
{
    #region Properties

    /// <summary>
    ///     Gets the timestamp when the entity was created.
    /// </summary>
    DateTimeOffset CreatedOn { get; }

    /// <summary>
    ///     Gets the timestamp when the entity was last updated.
    /// </summary>
    DateTimeOffset? UpdatedOn { get; }

    /// <summary>
    ///     Gets the identifier of the user who created the entity.
    /// </summary>
    string CreatedBy { get; }

    /// <summary>
    ///     Gets the identifier of the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; }

    #endregion
}
