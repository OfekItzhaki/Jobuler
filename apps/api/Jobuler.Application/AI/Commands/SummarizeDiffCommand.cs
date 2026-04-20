using MediatR;

namespace Jobuler.Application.AI.Commands;

public record SummarizeDiffCommand(
    DiffContextDto Diff,
    string Locale) : IRequest<string>;

public class SummarizeDiffCommandHandler : IRequestHandler<SummarizeDiffCommand, string>
{
    private readonly IAiAssistant _ai;

    public SummarizeDiffCommandHandler(IAiAssistant ai) => _ai = ai;

    public Task<string> Handle(SummarizeDiffCommand req, CancellationToken ct) =>
        _ai.SummarizeDiffAsync(req.Diff, req.Locale, ct);
}

public record ExplainInfeasibilityCommand(
    InfeasibilityContextDto Context,
    string Locale) : IRequest<string>;

public class ExplainInfeasibilityCommandHandler : IRequestHandler<ExplainInfeasibilityCommand, string>
{
    private readonly IAiAssistant _ai;

    public ExplainInfeasibilityCommandHandler(IAiAssistant ai) => _ai = ai;

    public Task<string> Handle(ExplainInfeasibilityCommand req, CancellationToken ct) =>
        _ai.ExplainInfeasibilityAsync(req.Context, req.Locale, ct);
}
