using Microsoft.AspNetCore.Authentication;
using SecureVotingApp.Services;

namespace secureVotingApp.Tests;

public class BcryptServiceTests
{
    [Fact]
    public void VerifyCredential_WithCorrectCredential_ReturnsTrue()
    {
        BcryptService bc = new BcryptService();
        //credential value after argon2 from front end
        string argon2Hash = "AppleSauce123!";
        //hash credential that will be stored
        string hashedCred = bc.HashCredential(argon2Hash);

        //comparison 
        bool result = bc.VerifyCredential(argon2Hash, hashedCred);

        Assert.True(result);
    }
}
