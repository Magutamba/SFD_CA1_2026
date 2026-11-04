using System.ComponentModel.DataAnnotations;

namespace SecureVotingApp.Components.Contracts;

public class LoginRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required] public string Password { get; set; } = string.Empty;
    [Required] [MaxLength(6)] public string Code { get; set; } = string.Empty;
}