using FluentValidation;
using Jobuler.Application.Spaces.Commands;

namespace Jobuler.Application.Spaces.Validators;

public class CreateSpaceCommandValidator : AbstractValidator<CreateSpaceCommand>
{
    private static readonly string[] AllowedLocales = ["he", "en", "ru"];

    public CreateSpaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Space name is required.")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);

        RuleFor(x => x.Locale)
            .Must(l => AllowedLocales.Contains(l))
            .WithMessage("Locale must be one of: he, en, ru.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");
    }
}
