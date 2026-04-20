using Jobuler.Domain.Common;

namespace Jobuler.Domain.People;

public class PersonQualification : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid PersonId { get; private set; }
    public string Qualification { get; private set; } = default!;
    public DateOnly? IssuedAt { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private PersonQualification() { }

    public static PersonQualification Create(
        Guid spaceId, Guid personId, string qualification,
        DateOnly? issuedAt = null, DateOnly? expiresAt = null) =>
        new()
        {
            SpaceId = spaceId,
            PersonId = personId,
            Qualification = qualification.Trim(),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };

    public void Deactivate() => IsActive = false;
}
