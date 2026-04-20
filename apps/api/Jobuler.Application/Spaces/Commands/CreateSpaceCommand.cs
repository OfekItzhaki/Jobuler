using MediatR;

namespace Jobuler.Application.Spaces.Commands;

public record CreateSpaceCommand(
    string Name,
    string? Description,
    string Locale,
    Guid RequestingUserId) : IRequest<Guid>;
