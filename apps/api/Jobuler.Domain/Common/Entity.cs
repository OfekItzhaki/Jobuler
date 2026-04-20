namespace Jobuler.Domain.Common;

/// <summary>
/// Base class for all domain entities with a UUID primary key.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
}

/// <summary>
/// Base class for entities that track the last update time.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
