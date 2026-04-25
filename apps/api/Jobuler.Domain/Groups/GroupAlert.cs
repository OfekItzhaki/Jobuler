using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

public enum AlertSeverity { Info, Warning, Critical }

public class GroupAlert : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public AlertSeverity Severity { get; private set; }
    public Guid CreatedByPersonId { get; private set; }

    private GroupAlert() { }

    public static GroupAlert Create(
        Guid spaceId, Guid groupId,
        string title, string body,
        AlertSeverity severity, Guid createdByPersonId) =>
        new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            Title = title.Trim(),
            Body = body.Trim(),
            Severity = severity,
            CreatedAt = DateTime.UtcNow,
            CreatedByPersonId = createdByPersonId
        };

    public void Update(string title, string body, AlertSeverity severity)
    {
        Title = title.Trim();
        Body = body.Trim();
        Severity = severity;
    }
}
