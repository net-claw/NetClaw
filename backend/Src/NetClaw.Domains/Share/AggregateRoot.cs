using NetClaw.EfCore.Extensions.Entities;

namespace NetClaw.Domains.Share;

public abstract class AggregateRoot : AuditedEntity<Guid>
{
    #region Constructors

    protected AggregateRoot(string? createdBy, DateTimeOffset? createdOn = null)
        : this(Guid.NewGuid(), createdBy, createdOn)
    {
    }

    protected AggregateRoot(Guid id, string? createdBy, DateTimeOffset? createdOn = null)
        : base(id)
    {
        SetCreatedBy(createdBy ?? "System", createdOn);
    }

    /// <inheritdoc />
    protected AggregateRoot()
    {
    }

    #endregion
}