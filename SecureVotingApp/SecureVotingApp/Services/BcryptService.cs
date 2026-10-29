// File: BcrytService.cs
// @uthor: Jeán Walton
// Date: 28/10/2026

namespace SecureVotingApp.Services;

//On initial sign up, Hashing credentials from front end using Bcrypt (even though the backend is receiving a Argon2)
public class BcryptService
{
    public string HashCredential(string argon2Hash)
    {
        string hashedCred = BCrypt.Net.BCrypt.HashPassword(argon2Hash);

        return hashedCred;
    }

    //verify supplied credentials against the stored hash during login into existing account
    public bool VerifyCredential(string argon2Hash, string storedHashedCred)
    {
        bool isVerified = BCrypt.Net.BCrypt.Verify(argon2Hash, storedHashedCred);
        
        return isVerified;
    }
}



