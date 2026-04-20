using FluentAssertions;
using FluentValidation.TestHelper;
using Jobuler.Application.Auth.Commands;
using Jobuler.Application.Auth.Validators;
using Xunit;

namespace Jobuler.Tests.Validation;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        var cmd = new RegisterCommand("test@example.com", "Test User", "Password1!", "he");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_Email_FailsValidation()
    {
        var cmd = new RegisterCommand("not-an-email", "Test User", "Password1!", "he");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Weak_Password_FailsValidation()
    {
        var cmd = new RegisterCommand("test@example.com", "Test User", "weak", "he");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_Without_Uppercase_FailsValidation()
    {
        var cmd = new RegisterCommand("test@example.com", "Test User", "password1!", "he");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Invalid_Locale_FailsValidation()
    {
        var cmd = new RegisterCommand("test@example.com", "Test User", "Password1!", "fr");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PreferredLocale);
    }

    [Theory]
    [InlineData("he")]
    [InlineData("en")]
    [InlineData("ru")]
    public void Valid_Locales_PassValidation(string locale)
    {
        var cmd = new RegisterCommand("test@example.com", "Test User", "Password1!", locale);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PreferredLocale);
    }
}
