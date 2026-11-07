using System.ComponentModel.DataAnnotations;
namespace SecureVotingApp.Components.Contracts;

public sealed class RegisterRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    // Regex explained:
    // ?= looks at entire string
    // . matches any character
    // * matches preceding expression zero or more times
    // a-z = lowercase letters, A-Z = uppercase characters, 0-9 = digits, \s or \S = whitespace
    [Required] [MinLength(12)] [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9\s])\S{12,}$", ErrorMessage = "Password must contain uppercase, lowercase, a number and a special character.")] public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword
    {
        get;
        set;
    } = string.Empty;
}