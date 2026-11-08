using System.ComponentModel.DataAnnotations;

namespace SecureVotingApp.Components.Contracts;

public class LoginRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required] public string Password { get; set; } = string.Empty;
    [Required] [MaxLength(6)] [RegularExpression(@"^[0-9]{6}$",ErrorMessage = "Authentication code must contain exactly 6 digits.")]public string Code { get; set; } = string.Empty;
}