// File: BcryptService.cs
// @uthor: Jeán Walton
// Date: 28/10/2026

namespace SecureVotingApp.Services;

//Hashes the Argon2id derived credential using bcrypt before storing it in the database
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



