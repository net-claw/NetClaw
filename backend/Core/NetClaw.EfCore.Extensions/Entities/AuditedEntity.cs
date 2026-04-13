using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetClaw.EfCore.Extensions.Entities;

/// <summary>
///     Base class for entities that track creation and modification metadata with a specified key type.
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
/// <remarks>
///     This type provides the standard audit fields used by the domain:
///     creator, creation time, updater, and update time.
/// </remarks>
public abstract class AuditedEntity<TKey> : Entity<TKey>,
    IAuditedEntity<TKey>
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuditedEntity{TKey}" /> class.
    /// </summary>
    protected AuditedEntity()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuditedEntity{TKey}" /> class with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected AuditedEntity(TKey id)
        : base(id)
    {
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the user who created this entity.
    /// </summary>
    [MaxLength(500)]
    public string CreatedBy { get; private set; } = null!;

    /// <summary>
    ///     Gets the timestamp when this entity was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; private set; }

    /// <summary>
    ///     Gets the user who last modified this entity, or the creator if never modified.
    /// </summary>
    [NotMapped]
    public string LastModifiedBy => UpdatedBy ?? CreatedBy;

    /// <summary>
    ///     Gets the timestamp when this entity was last modified, or the creation timestamp if never modified.
    /// </summary>
    [NotMapped]
    public DateTimeOffset LastModifiedOn => UpdatedOn ?? CreatedOn;

    /// <summary>
    ///     Gets the user who last modified this entity.
    /// </summary>
    [MaxLength(500)]
    public string? UpdatedBy { get; private set; }

    /// <summary>
    ///     Gets the timestamp when this entity was last modified.
    /// </summary>
    public DateTimeOffset? UpdatedOn { get; private set; }

    #endregion

    #region Methods

    /// <summary>
    ///     Sets the creation audit information for this entity.
    /// </summary>
    /// <param name="userName">The username of the creator.</param>
    /// <param name="createdOn">Optional creation timestamp. Defaults to UTC now if not specified.</param>
    /// <exception cref="ArgumentNullException">Thrown when userName is null.</exception>
    protected void SetCreatedBy(string userName, DateTimeOffset? createdOn = null)
    {
        if (!string.IsNullOrEmpty(CreatedBy)) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        CreatedBy = userName;
        CreatedOn = createdOn ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Sets the modification audit information for this entity.
    /// </summary>
    /// <param name="userName">The username of the modifier.</param>
    /// <param name="updatedOn">Optional modification timestamp. Defaults to UTC now if not specified.</param>
    /// <exception cref="ArgumentNullException">Thrown when userName is null or empty.</exception>
    protected void SetUpdatedBy(string userName, DateTimeOffset? updatedOn = null)
    {
        if (updatedOn < UpdatedOn) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        UpdatedBy = userName;
        UpdatedOn = updatedOn ?? DateTimeOffset.UtcNow;
    }

    #endregion
}

/// <summary>
///     Base class for auditable entities that use <see cref="Guid" /> as the primary key.
/// </summary>
public abstract class AuditedEntity : AuditedEntity<Guid>
{
    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuditedEntity" /> class.
    /// </summary>
    protected AuditedEntity()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuditedEntity" /> class with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected AuditedEntity(Guid id)
        : base(id)
    {
    }

    #endregion
}
