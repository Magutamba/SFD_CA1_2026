// File: ConfirmRegistrationDto.cs
// @uthor: Jeán Walton
// 31/10/2026

namespace SecureVotingApp.DTOs;

public class ConfirmRegistrationDto
{
    public string RegistrationId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}