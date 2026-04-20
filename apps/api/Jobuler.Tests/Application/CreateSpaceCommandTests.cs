using FluentAssertions;
using Jobuler.Application.Spaces.Commands;
using Jobuler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jobuler.Tests.Application;

public class CreateSpaceCommandTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_CreatesSpaceWithOwnerPermissions()
    {
        var db = CreateDb();
        var handler = new CreateSpaceCommandHandler(db);
        var userId = Guid.NewGuid();

        var spaceId = await handler.Handle(
            new CreateSpaceCommand("Test Space", null, "he", userId),
            CancellationToken.None);

        var space = await db.Spaces.FindAsync(spaceId);
        space.Should().NotBeNull();
        space!.OwnerUserId.Should().Be(userId);

        var permissions = await db.SpacePermissionGrants
            .Where(g => g.SpaceId == spaceId && g.UserId == userId)
            .ToListAsync();

        permissions.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public async Task Handle_CreatesMembership()
    {
        var db = CreateDb();
        var handler = new CreateSpaceCommandHandler(db);
        var userId = Guid.NewGuid();

        var spaceId = await handler.Handle(
            new CreateSpaceCommand("Test Space", null, "he", userId),
            CancellationToken.None);

        var membership = await db.SpaceMemberships
            .FirstOrDefaultAsync(m => m.SpaceId == spaceId && m.UserId == userId);

        membership.Should().NotBeNull();
    }
}
