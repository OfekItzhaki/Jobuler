using FluentAssertions;
using Jobuler.Domain.Tasks;
using Xunit;

namespace Jobuler.Tests.Domain;

public class TaskSlotTests
{
    [Fact]
    public void Create_WithEndsBeforeStarts_ThrowsArgumentException()
    {
        var start = DateTime.UtcNow.AddHours(8);
        var end = DateTime.UtcNow;

        var act = () => TaskSlot.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            start, end, 1, 5, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithValidTimes_Succeeds()
    {
        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddHours(8);

        var slot = TaskSlot.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            start, end, 2, 5, Guid.NewGuid());

        slot.RequiredHeadcount.Should().Be(2);
        slot.Status.Should().Be(TaskSlotStatus.Active);
    }

    [Fact]
    public void Cancel_SetsStatusToCancelled()
    {
        var slot = TaskSlot.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, DateTime.UtcNow.AddHours(8),
            1, 5, Guid.NewGuid());

        slot.Cancel();

        slot.Status.Should().Be(TaskSlotStatus.Cancelled);
    }
}
