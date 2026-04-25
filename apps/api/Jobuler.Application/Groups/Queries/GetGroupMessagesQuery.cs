using Jobuler.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Jobuler.Application.Groups.Queries;

public record GroupMessageDto(
    Guid Id, string Content, bool IsPinned,
    string AuthorName, DateTime CreatedAt);

public record GetGroupMessagesQuery(Guid SpaceId, Guid GroupId) : IRequest<List<GroupMessageDto>>;

public class GetGroupMessagesQueryHandler : IRequestHandler<GetGroupMessagesQuery, List<GroupMessageDto>>
{
    private readonly AppDbContext _db;
    public GetGroupMessagesQueryHandler(AppDbContext db) => _db = db;

    public async Task<List<GroupMessageDto>> Handle(GetGroupMessagesQuery req, CancellationToken ct)
    {
        var messages = await _db.GroupMessages.AsNoTracking()
            .Where(m => m.GroupId == req.GroupId && m.SpaceId == req.SpaceId)
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        var userIds = messages.Select(m => m.AuthorUserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        return messages.Select(m => new GroupMessageDto(
            m.Id, m.Content, m.IsPinned,
            users.GetValueOrDefault(m.AuthorUserId, "Unknown"),
            m.CreatedAt))
            .ToList();
    }
}
