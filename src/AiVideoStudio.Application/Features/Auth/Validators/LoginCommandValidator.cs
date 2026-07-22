using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Auth.Commands;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Application.Features.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(IOptions<PasswordPolicyOptions> passwordOptions)
    {
        var options = passwordOptions.Value;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        var passwordRule = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(options.MinLength).WithMessage($"Password must be at least {options.MinLength} characters.");

        if (options.RequireUppercase)
        {
            passwordRule.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
        }
        if (options.RequireLowercase)
        {
            passwordRule.Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.");
        }
        if (options.RequireDigit)
        {
            passwordRule.Matches("[0-9]").WithMessage("Password must contain at least one number.");
        }
        if (options.RequireSpecialCharacter)
        {
            passwordRule.Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }
}
