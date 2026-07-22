using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Application.Configuration;

public class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    [Range(6, 128)]
    public int MinLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
}
