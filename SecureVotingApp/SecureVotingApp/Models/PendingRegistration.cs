// File: PendingRegistration.cs
// @uthor: Jeán Walton
// 31/10/2026

namespace SecureVotingApp.Models;

public class PendingRegistration
{
    
   
    public string Email { get; set; } = string.Empty; 
    public string CredentialHash { get; set; } = string.Empty;//stored the bcrypt has of the Argon2id derived credential
    public string Argon2Salt { get; set; } = string.Empty;

    public required string TotpSecret { get; set; } 
    public int FailedAttempts { get; set; } = 0;//tracks incorrect TOTP confirmation attempts.

}