using FluentValidation;
using Jobuler.Application.People.Commands;

namespace Jobuler.Application.People.Validators;

public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2)
            .MaximumLength(150);
        RuleFor(x => x.DisplayName)
            .MaximumLength(100).When(x => x.DisplayName != null);
    }
}

public class AddRestrictionCommandValidator : AbstractValidator<AddRestrictionCommand>
{
    public AddRestrictionCommandValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.PersonId).NotEmpty();
        RuleFor(x => x.RestrictionType)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.EffectiveFrom)
            .NotEmpty();
        RuleFor(x => x.EffectiveUntil)
            .Must((cmd, until) => until == null || until >= cmd.EffectiveFrom)
            .WithMessage("EffectiveUntil must be on or after EffectiveFrom.");
        RuleFor(x => x.OperationalNote)
            .MaximumLength(500).When(x => x.OperationalNote != null);
        RuleFor(x => x.SensitiveReason)
            .MaximumLength(500).When(x => x.SensitiveReason != null);
    }
}
