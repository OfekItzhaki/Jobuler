namespace Jobuler.Application.Common;

/// <summary>
/// Checks whether a user holds a specific permission within a space.
/// Used by command/query handlers before performing privileged operations.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid spaceId, string permissionKey, CancellationToken ct = default);
    Task RequirePermissionAsync(Guid userId, Guid spaceId, string permissionKey, CancellationToken ct = default);
}
