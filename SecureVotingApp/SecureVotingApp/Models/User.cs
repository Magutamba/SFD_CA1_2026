// File: User.cs
// @uthor: Jeán Walton
// 28/10/2026

namespace SecureVotingApp.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty; 
    public string CredentialHash { get; set; } = string.Empty;//stored the bcrypt has of the Argon2id derived credential
    public string Argon2Salt { get; set; } = string.Empty;
    //stores the TOTP secret encrypted using ASP.NET Core Data Protection
    public required string TotpSecret { get; set; } 
    //records the last accepted TOTP time step to prevent code reuse
    public long? LastUsedTotpTimeStep { get; set; }
}