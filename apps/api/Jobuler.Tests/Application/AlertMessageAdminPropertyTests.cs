// Feature: admin-management-and-scheduling
// Property-based tests for Alert and Message admin operations (Task 31)

using FluentAssertions;
using Jobuler.Application.Common;
using Jobuler.Application.Groups.Commands;
using Jobuler.Domain.Groups;
using Jobuler.Domain.People;
using Jobuler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Jobuler.Tests.Application;

public class AlertMessageAdminPropertyTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IPermissionService AllowAllPermissions()
    {
        var svc = Substitute.For<IPermissionService>();
        svc.RequirePermissionAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        svc.HasPermissionAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        return svc;
    }

    private static async Task<Guid> SeedPersonAndMembership(
        AppDbContext db, Guid spaceId, Guid groupId, Guid userId)
    {
        var person = Person.Create(spaceId, "Test User", null, userId, null);
        db.People.Add(person);
        var membership = GroupMembership.Create(spaceId, groupId, person.Id, false);
        db.GroupMemberships.Add(membership);
        await db.SaveChangesAsync();
        return person.Id;
    }

    // ── Property 10: Create alert as user A → delete as user B → 204 and gone ─
    // Validates: Requirements 6.1, 6.3
    // Feature: admin-management-and-scheduling, Property 10: any admin can delete any alert

    [Theory]
    [InlineData("info")]
    [InlineData("warning")]
    [InlineData("critical")]
    [InlineData("info",    "Alert Title A", "Alert Body A")]
    [InlineData("warning", "Alert Title B", "Alert Body B")]
    public async Task Property10_CreateAlertAsUserA_DeleteAsUserB_AlertGone(string severity,
        string title = "Alert Title", string body = "Alert Body")
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userA = Guid.NewGuid(); // creator
        var userB = Guid.NewGuid(); // different admin

        var personAId = await SeedPersonAndMembership(db, spaceId, groupId, userA);
        await SeedPersonAndMembership(db, spaceId, groupId, userB);

        Enum.TryParse<AlertSeverity>(severity, ignoreCase: true, out var sev);
        var alert = GroupAlert.Create(spaceId, groupId, title, body, sev, personAId);
        db.GroupAlerts.Add(alert);
        await db.SaveChangesAsync();

        var handler = new DeleteGroupAlertCommandHandler(db, AllowAllPermissions());

        // Act — user B (not the creator) deletes user A's alert
        var act = async () => await handler.Handle(
            new DeleteGroupAlertCommand(spaceId, groupId, alert.Id, userB),
            CancellationToken.None);

        // Assert — no exception (204 equivalent); alert is gone
        await act.Should().NotThrowAsync();
        db.GroupAlerts.Should().HaveCount(0);
        (await db.GroupAlerts.FindAsync(alert.Id)).Should().BeNull();
    }

    // ── Property 11: Create message as user A → delete as user B → 204 and gone
    // Validates: Requirements 7.1, 7.2
    // Feature: admin-management-and-scheduling, Property 11: admin can delete any message

    [Theory]
    [InlineData("Hello world")]
    [InlineData("Important announcement")]
    [InlineData("Schedule update")]
    [InlineData("Emergency notice")]
    [InlineData("Routine message")]
    public async Task Property11_CreateMessageAsUserA_DeleteAsUserB_MessageGone(string content)
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userA = Guid.NewGuid(); // author
        var userB = Guid.NewGuid(); // admin (not the author)

        var message = GroupMessage.Create(spaceId, groupId, userA, content, false);
        db.GroupMessages.Add(message);
        await db.SaveChangesAsync();

        var handler = new DeleteGroupMessageCommandHandler(db, AllowAllPermissions());

        // Act — user B (not the author) deletes user A's message
        var act = async () => await handler.Handle(
            new DeleteGroupMessageCommand(spaceId, groupId, message.Id, userB),
            CancellationToken.None);

        // Assert — no exception; message is gone
        await act.Should().NotThrowAsync();
        db.GroupMessages.Should().HaveCount(0);
        (await db.GroupMessages.FindAsync(message.Id)).Should().BeNull();
    }

    // ── Property 12: Create message → pin → unpin → isPinned = false ──────────
    // Validates: Requirements 8.1, 8.2
    // Feature: admin-management-and-scheduling, Property 12: pin/unpin round-trip

    [Theory]
    [InlineData("Hello world")]
    [InlineData("Important announcement")]
    [InlineData("Schedule update")]
    [InlineData("Emergency notice")]
    [InlineData("Routine message")]
    public async Task Property12_CreateMessage_Pin_Unpin_IsPinnedFalse(string content)
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var message = GroupMessage.Create(spaceId, groupId, userId, content, isPinned: false);
        db.GroupMessages.Add(message);
        await db.SaveChangesAsync();

        var handler = new PinGroupMessageCommandHandler(db, AllowAllPermissions());

        // Act — pin
        await handler.Handle(
            new PinGroupMessageCommand(spaceId, groupId, message.Id, userId, IsPinned: true),
            CancellationToken.None);

        var pinned = await db.GroupMessages.FindAsync(message.Id);
        pinned!.IsPinned.Should().BeTrue();

        // Act — unpin
        await handler.Handle(
            new PinGroupMessageCommand(spaceId, groupId, message.Id, userId, IsPinned: false),
            CancellationToken.None);

        // Assert — isPinned = false
        var unpinned = await db.GroupMessages.FindAsync(message.Id);
        unpinned!.IsPinned.Should().BeFalse();
    }

    // ── Property 13: Create alert → update → fetch → fields match ────────────
    // Validates: Requirements 6.2
    // Feature: admin-management-and-scheduling, Property 13: alert update round-trip

    [Theory]
    [InlineData("Original Title", "Original Body", "info",     "Updated Title",   "Updated Body",   "warning")]
    [InlineData("Alert A",        "Body A",         "warning",  "Alert A Updated", "Body A Updated", "critical")]
    [InlineData("Emergency",      "All hands",      "critical", "Resolved",        "Stand down",     "info")]
    [InlineData("Notice",         "Short body",     "info",     "Notice Updated",  "Longer body now","info")]
    [InlineData("  Padded  ",     "  Body  ",       "warning",  "Clean Title",     "Clean Body",     "critical")]
    public async Task Property13_CreateAlert_Update_FieldsMatch(
        string origTitle, string origBody, string origSeverity,
        string newTitle, string newBody, string newSeverity)
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var personId = await SeedPersonAndMembership(db, spaceId, groupId, userId);

        Enum.TryParse<AlertSeverity>(origSeverity, ignoreCase: true, out var origSev);
        var alert = GroupAlert.Create(spaceId, groupId, origTitle, origBody, origSev, personId);
        db.GroupAlerts.Add(alert);
        await db.SaveChangesAsync();

        var updateHandler = new UpdateGroupAlertCommandHandler(db, AllowAllPermissions());

        var cmd = new UpdateGroupAlertCommand(
            spaceId, groupId, alert.Id, userId,
            newTitle, newBody, newSeverity);

        // Act
        await updateHandler.Handle(cmd, CancellationToken.None);

        // Assert — fetch directly from DB
        var updated = await db.GroupAlerts.FindAsync(alert.Id);
        updated!.Title.Should().Be(newTitle.Trim());
        updated.Body.Should().Be(newBody.Trim());
        updated.Severity.ToString().ToLowerInvariant().Should().Be(newSeverity.ToLowerInvariant());
    }

    // ── Property 14: Create message → update content → fetch → content matches ─
    // Validates: Requirements 7.3
    // Feature: admin-management-and-scheduling, Property 14: message update round-trip

    [Theory]
    [InlineData("Original content",    "Updated content")]
    [InlineData("Hello",               "Goodbye")]
    [InlineData("Short",               "This is a much longer updated message content")]
    [InlineData("  Padded content  ",  "Clean content")]
    [InlineData("First version",       "Second version")]
    public async Task Property14_CreateMessage_UpdateContent_ContentMatches(
        string originalContent, string newContent)
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var message = GroupMessage.Create(spaceId, groupId, userId, originalContent, false);
        db.GroupMessages.Add(message);
        await db.SaveChangesAsync();

        var updateHandler = new UpdateGroupMessageCommandHandler(db, AllowAllPermissions());

        var cmd = new UpdateGroupMessageCommand(
            spaceId, groupId, message.Id, userId, newContent);

        // Act
        await updateHandler.Handle(cmd, CancellationToken.None);

        // Assert — fetch directly from DB
        var updated = await db.GroupMessages.FindAsync(message.Id);
        updated!.Content.Should().Be(newContent.Trim());
    }
}
