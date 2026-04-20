namespace Jobuler.Domain.Common;

/// <summary>
/// Marks a domain entity as belonging to a specific space (tenant).
/// All tenant-scoped entities must implement this interface.
/// </summary>
public interface ITenantScoped
{
    Guid SpaceId { get; }
}
