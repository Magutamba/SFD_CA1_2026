// @author Lorenzo

using System.ComponentModel.DataAnnotations;

namespace SecureVotingApp.Client.Contracts.AccountContracts;

public class ConfirmRegistrationRequest
{
    // Filled by backend reply
    public string RegistrationId { get; set; } = string.Empty;
    // User input
    // Regex Explained:
    // ^[...] all characters must match pattern
    // 0-9 = digits
    [Required] [MaxLength(6)] [RegularExpression(@"^[0-9]{6}$",ErrorMessage = "Authentication code must contain exactly 6 digits.")]
    public string Code { get; set; } = string.Empty;
}