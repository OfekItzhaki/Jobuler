using FluentValidation;
using Jobuler.Application.Constraints.Commands;

namespace Jobuler.Application.Constraints.Validators;

public class CreateConstraintCommandValidator : AbstractValidator<CreateConstraintCommand>
{
    public CreateConstraintCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.RuleType)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.RulePayloadJson)
            .NotEmpty()
            .Must(BeValidJson).WithMessage("RulePayloadJson must be valid JSON.");
        RuleFor(x => x.EffectiveUntil)
            .Must((cmd, until) => until == null || cmd.EffectiveFrom == null || until >= cmd.EffectiveFrom)
            .WithMessage("EffectiveUntil must be on or after EffectiveFrom.");
    }

    private static bool BeValidJson(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
