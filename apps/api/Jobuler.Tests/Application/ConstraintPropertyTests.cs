// Feature: admin-management-and-scheduling
// Property-based tests for Constraint CRUD (Task 30)

using FluentAssertions;
using Jobuler.Application.Common;
using Jobuler.Application.Constraints.Commands;
using Jobuler.Application.Constraints.Queries;
using Jobuler.Domain.Constraints;
using Jobuler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Jobuler.Tests.Application;

public class ConstraintPropertyTests
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
        return svc;
    }

    private static async Task<Guid> SeedConstraint(
        AppDbContext db, Guid spaceId, string rulePayloadJson = "{}")
    {
        var rule = ConstraintRule.Create(
            spaceId, ConstraintScopeType.Space, null,
            ConstraintSeverity.Hard, "min_rest_hours",
            rulePayloadJson, Guid.NewGuid(),
            new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
        db.ConstraintRules.Add(rule);
        await db.SaveChangesAsync();
        return rule.Id;
    }

    // ── Property 6: Create constraint → update → verify fields match ──────────
    // Validates: Requirements 2.1, 2.2
    // Feature: admin-management-and-scheduling, Property 6: constraint update round-trip

    [Theory]
    [InlineData("{\"hours\": 8}",  "2025-01-01", "2025-12-31")]
    [InlineData("{\"hours\": 12}", "2025-03-01", "2025-09-30")]
    [InlineData("{\"max\": 2}",    "2025-06-01", "2025-06-30")]
    [InlineData("{}",              "2025-01-01", "2025-06-01")]
    [InlineData("{\"min\": 3, \"window_hours\": 24}", "2025-04-01", "2025-10-31")]
    public async Task Property6_UpdateConstraint_FieldsMatch(
        string newPayload, string effectiveFromStr, string effectiveUntilStr)
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var constraintId = await SeedConstraint(db, spaceId);

        var handler = new UpdateConstraintCommandHandler(db, AllowAllPermissions());
        var getHandler = new GetConstraintsQueryHandler(db);

        var effectiveFrom = DateOnly.Parse(effectiveFromStr);
        var effectiveUntil = DateOnly.Parse(effectiveUntilStr);

        var cmd = new UpdateConstraintCommand(
            spaceId, constraintId, userId,
            newPayload, null, effectiveFrom, effectiveUntil);

        // Act
        await handler.Handle(cmd, CancellationToken.None);
        var constraints = await getHandler.Handle(new GetConstraintsQuery(spaceId), CancellationToken.None);

        // Assert
        constraints.Should().HaveCount(1);
        var c = constraints[0];
        c.Id.Should().Be(constraintId);
        c.RulePayloadJson.Should().Be(newPayload);
        c.EffectiveUntil.Should().Be(effectiveUntil);
    }

    // ── Property 7: Non-JSON strings → validator rejects ─────────────────────
    // Validates: Requirements 2.3
    // Feature: admin-management-and-scheduling, Property 7: invalid JSON rejected by validator

    [Theory]
    [InlineData("not json")]
    [InlineData("123abc")]
    [InlineData("{bad}")]
    [InlineData("just text")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("undefined")]
    [InlineData("[unclosed")]
    [InlineData("{\"key\": }")]
    [InlineData("plain text")]
    public void Property7_Validator_RejectsNonJsonStrings(string badJson)
    {
        // Arrange
        var validator = new UpdateConstraintCommandValidator();
        var cmd = new UpdateConstraintCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            badJson,
            null,
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31));

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    // ── Property 8: effectiveUntil < effectiveFrom → validator rejects ────────
    // Validates: Requirements 2.4
    // Feature: admin-management-and-scheduling, Property 8: invalid date range rejected

    [Theory]
    [InlineData("2025-06-01", "2025-01-01")]  // until well before from
    [InlineData("2025-06-01", "2025-05-31")]  // until one day before from
    [InlineData("2025-12-31", "2025-01-01")]  // until at start of year, from at end
    [InlineData("2026-01-01", "2025-12-31")]  // until one day before from
    [InlineData("2025-07-15", "2025-07-14")]  // until one day before from
    public void Property8_Validator_RejectsEffectiveUntilBeforeEffectiveFrom(
        string effectiveFromStr, string effectiveUntilStr)
    {
        // Arrange
        var validator = new UpdateConstraintCommandValidator();
        var effectiveFrom = DateOnly.Parse(effectiveFromStr);
        var effectiveUntil = DateOnly.Parse(effectiveUntilStr);

        var cmd = new UpdateConstraintCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "{}",
            null,
            effectiveFrom, effectiveUntil);

        // Act
        var result = validator.Validate(cmd);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    // ── Property 9: Create constraint → delete → list → absent ───────────────
    // Validates: Requirements 2.5
    // Feature: admin-management-and-scheduling, Property 9: delete removes constraint from list

    [Theory]
    [InlineData("{\"hours\": 8}")]
    [InlineData("{\"max\": 2}")]
    [InlineData("{}")]
    [InlineData("{\"min\": 3}")]
    [InlineData("{\"burden_level\": \"disliked\"}")]
    public async Task Property9_CreateConstraint_Delete_NotInList(string payload)
    {
        // Arrange
        var db = CreateDb();
        var spaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var constraintId = await SeedConstraint(db, spaceId, payload);

        var deleteHandler = new DeleteConstraintCommandHandler(db, AllowAllPermissions());
        var getHandler = new GetConstraintsQueryHandler(db);

        // Verify it exists first
        var before = await getHandler.Handle(new GetConstraintsQuery(spaceId), CancellationToken.None);
        before.Should().HaveCount(1);

        // Act — delete
        await deleteHandler.Handle(
            new DeleteConstraintCommand(spaceId, constraintId, userId),
            CancellationToken.None);

        // Assert — absent from active list
        var after = await getHandler.Handle(new GetConstraintsQuery(spaceId, ActiveOnly: true), CancellationToken.None);
        after.Should().BeEmpty();
        after.Should().NotContain(c => c.Id == constraintId);
    }
}
