using Jobuler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Jobuler.Api.Middleware;

/// <summary>
/// Reads the spaceId from the route and the userId from the JWT claim,
/// then sets PostgreSQL session variables so RLS policies can filter rows.
/// Must run after authentication middleware.
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var spaceId = context.Request.RouteValues["spaceId"]?.ToString();

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(spaceId))
        {
            // Set PostgreSQL session variables for RLS policies
            await db.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.current_user_id', {0}, TRUE), set_config('app.current_space_id', {1}, TRUE)",
                userId, spaceId);
        }
        else if (!string.IsNullOrEmpty(userId))
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.current_user_id', {0}, TRUE)",
                userId);
        }

        await _next(context);
    }
}
