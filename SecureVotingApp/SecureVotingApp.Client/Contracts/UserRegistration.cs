namespace SecureVotingApp.Components.Contracts;

public sealed class UserRegistration
{
    public string Email { get; set; } = string.Empty;
    public string Argon2Hash { get; set; } = string.Empty;
    public string Argon2Salt { get; set; } = string.Empty;
}