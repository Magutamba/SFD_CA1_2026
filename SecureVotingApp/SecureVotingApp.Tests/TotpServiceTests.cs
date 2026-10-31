// File: User.cs
// @uthor: Jeán Walton
// 28/10/2026

using Xunit;
using OtpNet;
using SecureVotingApp.Services;

namespace SecureVotingApp.Tests;

public class TotpServiceTests
{
    private readonly TotpService _totpService = new TotpService();
    
    [Fact]
    public void VerifyTOTPCode_WithValidCode_ReturnsTrue()
    {
        // Arrange
        string secret = _totpService.GenerateSecret();

        byte[] secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        string code = totp.ComputeTotp();

        // Act
        bool result = _totpService.VerifyTOTPCode(secret, code);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyTOTPCode_WithInvalidCode_ReturnsFalse()
    {
        // Arrange
        string secret = _totpService.GenerateSecret();
        string invalidCode = "ABCDEF";

        // Act
        bool result = _totpService.VerifyTOTPCode(secret, invalidCode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyTOTPCode_WithIncorrectSixDigitCode_ReturnsFalse()
    {
        // Arrange
        string secret = _totpService.GenerateSecret();

        // Generate a code using a different secret
        string differentSecret = _totpService.GenerateSecret();

        byte[] differentSecretBytes = Base32Encoding.ToBytes(differentSecret);
        var otherTotp = new Totp(differentSecretBytes);

        string incorrectCode = otherTotp.ComputeTotp();

        // Act
        bool result = _totpService.VerifyTOTPCode(secret, incorrectCode);

        // Assert
        Assert.False(result);
    }
    
}       