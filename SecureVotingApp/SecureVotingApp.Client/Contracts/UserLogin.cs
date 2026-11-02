namespace SecureVotingApp.Components.Contracts;

public sealed class UserLogin
{
    public string Email { get; set; } = string.Empty;
    public string Argon2Hash { get; set; } = string.Empty;
}