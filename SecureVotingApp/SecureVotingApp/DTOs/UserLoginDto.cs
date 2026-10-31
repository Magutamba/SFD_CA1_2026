// File: UserLoginDto.cs
// @uthor: Jeán Walton
// 29/10/2026

namespace SecureVotingApp.DTOs;

public class UserLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Argon2Hash { get; set; } = string.Empty;

    public required string Code { get; set; } = string.Empty;

}