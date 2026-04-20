using MediatR;

namespace Jobuler.Application.AI.Commands;

/// <summary>
/// Step 1 of the AI constraint flow: parse natural language into a candidate constraint.
/// The result is returned to the admin for review — nothing is saved yet.
/// Step 2 is CreateConstraintCommand (existing) after admin confirms.
/// </summary>
public record ParseConstraintCommand(
    Guid SpaceId,
    string NaturalLanguageInput,
    string Locale,
    Guid RequestingUserId) : IRequest<ParsedConstraintDto>;

public class ParseConstraintCommandHandler : IRequestHandler<ParseConstraintCommand, ParsedConstraintDto>
{
    private readonly IAiAssistant _ai;

    public ParseConstraintCommandHandler(IAiAssistant ai) => _ai = ai;

    public Task<ParsedConstraintDto> Handle(ParseConstraintCommand req, CancellationToken ct) =>
        _ai.ParseConstraintAsync(req.NaturalLanguageInput, req.Locale, ct);
}
