// File: User.cs
// @uthor: Jeán Walton
// 28/10/2026

using Xunit;
using OtpNet;
using SecureVotingApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.DataProtection;

namespace SecureVotingApp.Tests;

public class TotpServiceTests
{   
    // Provides encryption services for testing.(buildin dotnet)
    private readonly IDataProtectionProvider provider =
    DataProtectionProvider.Create("SecureVotingApp.Tests");
    private readonly TotpService _totpService;
    
    public TotpServiceTests()
    {
        _totpService = new TotpService(provider);
    }
   
    [Fact]
    public void VerifyTOTPCode_WithValidCode_ReturnsTrue()
    {
        // Arrange
        string secret = _totpService.GenerateSecret();

        byte[] secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);

        string code = totp.ComputeTotp();

        // Act
        bool result = _totpService.VerifyTOTPCode(secret, code, out long timeStepMatched);

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
        bool result = _totpService.VerifyTOTPCode(secret, invalidCode, out long timeStepMatched);

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
        bool result = _totpService.VerifyTOTPCode(secret, incorrectCode, out long timeStepMatched);

        // Assert
        Assert.False(result);
    }
    
}       