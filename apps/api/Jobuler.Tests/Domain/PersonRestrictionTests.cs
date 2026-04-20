using FluentAssertions;
using Jobuler.Domain.People;
using Xunit;

namespace Jobuler.Tests.Domain;

public class PersonRestrictionTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var restriction = PersonRestriction.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "no_kitchen",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            "Cannot do kitchen", null, Guid.NewGuid());

        restriction.RestrictionType.Should().Be("no_kitchen");
        restriction.EffectiveUntil.Should().NotBeNull();
    }
}
