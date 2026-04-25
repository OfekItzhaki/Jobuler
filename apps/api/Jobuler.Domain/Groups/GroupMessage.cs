using Jobuler.Domain.Common;

namespace Jobuler.Domain.Groups;

public class GroupMessage : Entity, ITenantScoped
{
    public Guid SpaceId { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = default!;
    public bool IsPinned { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private GroupMessage() { }

    public static GroupMessage Create(Guid spaceId, Guid groupId, Guid authorUserId, string content, bool isPinned = false) =>
        new()
        {
            SpaceId = spaceId,
            GroupId = groupId,
            AuthorUserId = authorUserId,
            Content = content.Trim(),
            IsPinned = isPinned,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string content, bool isPinned)
    {
        Content = content.Trim();
        IsPinned = isPinned;
        UpdatedAt = DateTime.UtcNow;
    }
}
