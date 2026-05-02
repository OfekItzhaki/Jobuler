// Feature: group-ownership
// Property tests P1–P10 for group ownership model.
// Validates: Tasks 19.1, 19.2 from group-ownership spec

using FluentAssertions;
using Jobuler.Application.Common;
using Jobuler.Application.Groups.Commands;
using Jobuler.Application.Groups.Queries;
using Jobuler.Domain.Groups;
using Jobuler.Domain.Identity;
using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Jobuler.Tests.Groups;

public class GroupOwnershipPropertyTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IEmailSender NoOpEmail()
    {
        var e = Substitute.For<IEmailSender>();
        e.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return e;
    }

    /// <summary>
    /// Seeds a user + linked person in the given space.
    /// Returns (db, spaceId, userId, personId).
    /// </summary>
    private static async Task<(AppDbContext db, Guid spaceId, Guid userId, Guid personId)> SetupAsync()
    {
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var user = User.Create("test@test.com", "Test User", "hash");
        typeof(Jobuler.Domain.Common.Entity).GetProperty("Id")!.SetValue(user, userId);
        db.Users.Add(user);

        var person = Person.Create(spaceId, "Test User", linkedUserId: userId);
        db.People.Add(person);

        await db.SaveChangesAsync();
        return (db, spaceId, userId, person.Id);
    }

    private static async Task<Guid> CreateGroupAsync(AppDbContext db, Guid spaceId, Guid userId, string name = "Test Group")
    {
        var handler = new CreateGroupCommandHandler(db);
        return await handler.Handle(
            new CreateGroupCommand(spaceId, null, name, null, userId),
            CancellationToken.None);
    }

    // ── Property 1: Creator auto-membership ──────────────────────────────────
    // Feature: group-ownership, Property 1: creator auto-membership

    [Fact]
    public async Task Property1_CreateGroup_CreatorIsOwnerMember()
    {
        var (db, spaceId, userId, _) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var membership = await db.GroupMemberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.GroupId == groupId);

        membership.Should().NotBeNull("creator should be auto-added as member");
        membership!.IsOwner.Should().BeTrue("creator should be the owner");
    }

    // ── Property 2: Exactly one owner per group ───────────────────────────────
    // Feature: group-ownership, Property 2: exactly one owner per group

    [Fact]
    public async Task Property2_ExactlyOneOwnerPerGroup()
    {
        var (db, spaceId, userId, _) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var ownerCount = await db.GroupMemberships.AsNoTracking()
            .CountAsync(m => m.GroupId == groupId && m.IsOwner);

        ownerCount.Should().Be(1, "exactly one owner per group");
    }

    // ── Property 3: Owner removal rejected ───────────────────────────────────
    // Feature: group-ownership, Property 3: owner removal rejected

    [Fact]
    public async Task Property3_RemoveOwner_ThrowsInvalidOperation()
    {
        var (db, spaceId, userId, personId) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var handler = new RemovePersonFromGroupCommandHandler(db);

        var act = () => handler.Handle(
            new RemovePersonFromGroupCommand(spaceId, groupId, personId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*owner*");
    }

    // ── Property 4: Soft-deleted groups excluded from GetGroupsQuery ──────────
    // Feature: group-ownership, Property 4: soft-deleted groups excluded

    [Fact]
    public async Task Property4_SoftDeletedGroup_ExcludedFromQuery()
    {
        var (db, spaceId, userId, _) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var queryHandler = new GetGroupsQueryHandler(db);

        // Verify it appears before deletion
        var before = await queryHandler.Handle(new GetGroupsQuery(spaceId), CancellationToken.None);
        before.Should().Contain(g => g.Id == groupId);

        // Soft delete
        var deleteHandler = new SoftDeleteGroupCommandHandler(db);
        await deleteHandler.Handle(new SoftDeleteGroupCommand(spaceId, groupId, userId), CancellationToken.None);

        // Verify it's excluded after deletion
        var after = await queryHandler.Handle(new GetGroupsQuery(spaceId), CancellationToken.None);
        after.Should().NotContain(g => g.Id == groupId, "soft-deleted group should be excluded");
    }

    // ── Property 5: Soft-delete preserves membership rows ────────────────────
    // Feature: group-ownership, Property 5: soft-delete preserves memberships

    [Fact]
    public async Task Property5_SoftDelete_PreservesMembershipRows()
    {
        var (db, spaceId, userId, _) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var countBefore = await db.GroupMemberships.CountAsync(m => m.GroupId == groupId);

        var deleteHandler = new SoftDeleteGroupCommandHandler(db);
        await deleteHandler.Handle(new SoftDeleteGroupCommand(spaceId, groupId, userId), CancellationToken.None);

        var countAfter = await db.GroupMemberships.CountAsync(m => m.GroupId == groupId);

        countAfter.Should().Be(countBefore, "soft-delete must not remove membership rows");
    }

    // ── Property 6: Soft-delete / restore round trip ──────────────────────────
    // Feature: group-ownership, Property 6: soft-delete restore round trip

    [Fact]
    public async Task Property6_SoftDeleteRestore_GroupReappearsInQuery()
    {
        var (db, spaceId, userId, _) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var deleteHandler = new SoftDeleteGroupCommandHandler(db);
        var restoreHandler = new RestoreGroupCommandHandler(db, NoOpEmail());
        var queryHandler = new GetGroupsQueryHandler(db);

        await deleteHandler.Handle(new SoftDeleteGroupCommand(spaceId, groupId, userId), CancellationToken.None);
        await restoreHandler.Handle(new RestoreGroupCommand(spaceId, groupId, userId), CancellationToken.None);

        var groups = await queryHandler.Handle(new GetGroupsQuery(spaceId), CancellationToken.None);
        groups.Should().Contain(g => g.Id == groupId, "restored group should reappear");
    }

    // ── Property 9: Non-owner rejection ──────────────────────────────────────
    // Feature: group-ownership, Property 9: non-owner rejection

    [Theory]
    [InlineData("rename")]
    [InlineData("delete")]
    public async Task Property9_NonOwner_OwnerOnlyCommandsThrowUnauthorized(string command)
    {
        var (db, spaceId, userId, _) = await SetupAsync();

        var groupId = await CreateGroupAsync(db, spaceId, userId);

        // Create a second user who is NOT the owner
        var nonOwnerUserId = Guid.NewGuid();
        var nonOwnerUser = User.Create("other@test.com", "Other User", "hash");
        typeof(Jobuler.Domain.Common.Entity).GetProperty("Id")!.SetValue(nonOwnerUser, nonOwnerUserId);
        db.Users.Add(nonOwnerUser);
        var nonOwnerPerson = Person.Create(spaceId, "Other User", linkedUserId: nonOwnerUserId);
        db.People.Add(nonOwnerPerson);
        await db.SaveChangesAsync();

        Func<Task> act = command switch
        {
            "rename" => () => new RenameGroupCommandHandler(db).Handle(
                new RenameGroupCommand(spaceId, groupId, nonOwnerUserId, "New Name"), CancellationToken.None),
            "delete" => () => new SoftDeleteGroupCommandHandler(db).Handle(
                new SoftDeleteGroupCommand(spaceId, groupId, nonOwnerUserId), CancellationToken.None),
            _ => throw new ArgumentException()
        };

        await act.Should().ThrowAsync<UnauthorizedAccessException>(
            $"{command} by non-owner should throw UnauthorizedAccessException");
    }

    // ── Property 10: Rename rejects blank or >100-char names ─────────────────
    // Feature: group-ownership, Property 10: rename rejects invalid names
    // Note: RenameGroupCommand uses FluentValidation — the validator rejects these.
    // In unit tests without the MediatR pipeline, we test the domain entity directly.

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Property10_Rename_RejectsBlankNames(string invalidName)
    {
        var (db, spaceId, userId, _) = await SetupAsync();
        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var group = await db.Groups.FindAsync(groupId);
        var act = () => { group!.Rename(invalidName); return Task.CompletedTask; };

        await act.Should().ThrowAsync<Exception>("blank name should be rejected by domain entity");
    }

    [Fact]
    public async Task Property10_Rename_RejectsNameOver100Chars()
    {
        var (db, spaceId, userId, _) = await SetupAsync();
        var groupId = await CreateGroupAsync(db, spaceId, userId);

        var group = await db.Groups.FindAsync(groupId);
        var longName = new string('a', 101);
        var act = () => { group!.Rename(longName); return Task.CompletedTask; };

        await act.Should().ThrowAsync<Exception>("name over 100 chars should be rejected");
    }
}
