// @author Lorenzo

using System.ComponentModel.DataAnnotations;

namespace SecureVotingApp.Client.Contracts.AccountContracts;

public class ConfirmRegistrationRequest
{
    // Filled by backend reply
    public string RegistrationId { get; set; } = string.Empty;
    // User input
    [Required][MaxLength(6)]
    public string Code { get; set; } = string.Empty;
}