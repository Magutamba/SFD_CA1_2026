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


}