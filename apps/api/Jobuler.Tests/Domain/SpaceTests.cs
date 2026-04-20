using FluentAssertions;
using Jobuler.Domain.Spaces;
using Xunit;

namespace Jobuler.Tests.Domain;

public class SpaceTests
{
    [Fact]
    public void Create_WithValidData_SetsOwner()
    {
        var ownerId = Guid.NewGuid();
        var space = Space.Create("Test Space", ownerId, "desc", "he");

        space.OwnerUserId.Should().Be(ownerId);
        space.Name.Should().Be("Test Space");
        space.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => Space.Create("", Guid.NewGuid());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TransferOwnership_UpdatesOwner()
    {
        var originalOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        var space = Space.Create("Space", originalOwner);

        space.TransferOwnership(newOwner);

        space.OwnerUserId.Should().Be(newOwner);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var space = Space.Create("Space", Guid.NewGuid());
        space.Deactivate();
        space.IsActive.Should().BeFalse();
    }
}
