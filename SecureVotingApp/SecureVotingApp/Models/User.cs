// File: User.cs
// @uthor: Jeán Walton
// 28/10/2026

namespace SecureVotingApp.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty; //strings must be initialised
    public string CredentialHash { get; set; } = string.Empty;//argon2hash
    public string Argon2Salt { get; set; } = string.Empty;
    public required string TotpSecret { get; set; } //2FA
    //stores the last successfully used TOTP (time step) to prevent code reuse.
    public long? LastUsedTotpTimeStep { get; set; }
}