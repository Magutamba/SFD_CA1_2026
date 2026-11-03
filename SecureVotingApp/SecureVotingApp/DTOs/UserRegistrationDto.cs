// File: UserRegistrationDto.cs
// @uthor: Jeán Walton
// 29/10/2026

namespace SecureVotingApp.DTOs;
//defines the information received form the frontend
public class UserRegistrationDto
{
    public string Email { get; set; } = string.Empty;
    public string Argon2Hash { get; set; } = string.Empty;
    public string Argon2Salt { get; set; } = string.Empty;

}

