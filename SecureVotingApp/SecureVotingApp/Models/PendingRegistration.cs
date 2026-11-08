// File: PendingRegistration.cs
// @uthor: Jeán Walton
// 31/10/2026

namespace SecureVotingApp.Models;

public class PendingRegistration
{
    
   
    public string Email { get; set; } = string.Empty; //strings must be initialised
    public string CredentialHash { get; set; } = string.Empty;//argon2hash
    public string Argon2Salt { get; set; } = string.Empty;

    public required string TotpSecret { get; set; }

    // Number of incorrect 2FA codes submitted during registration.
    public int FailedAttempts { get; set; } = 0;

}