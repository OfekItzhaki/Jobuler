using MediatR;

namespace Jobuler.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string DisplayName,
    string Password,
    string PreferredLocale = "he",
    string? PhoneNumber = null) : IRequest<Guid>;
