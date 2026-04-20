using FluentAssertions;
using Jobuler.Domain.People;
using Xunit;

namespace Jobuler.Tests.Domain;

public class PresenceWindowTests
{
    [Fact]
    public void CreateManual_WithOnMission_ThrowsInvalidOperation()
    {
        var act = () => PresenceWindow.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(),
            PresenceState.OnMission,
            DateTime.UtcNow, DateTime.UtcNow.AddHours(8));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*derived*");
    }

    [Fact]
    public void CreateDerived_SetsOnMissionState()
    {
        var window = PresenceWindow.CreateDerived(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, DateTime.UtcNow.AddHours(8));

        window.State.Should().Be(PresenceState.OnMission);
        window.IsDerived.Should().BeTrue();
    }

    [Fact]
    public void CreateManual_WithEndsBeforeStarts_ThrowsArgumentException()
    {
        var act = () => PresenceWindow.CreateManual(
            Guid.NewGuid(), Guid.NewGuid(),
            PresenceState.AtHome,
            DateTime.UtcNow.AddHours(8), DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }
}
