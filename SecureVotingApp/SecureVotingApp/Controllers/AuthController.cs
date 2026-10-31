// File: AuthController.cs
// @uthor: Jeán Walton
// 29/10/2026

using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureVotingApp.Data;
using SecureVotingApp.DTOs;
using SecureVotingApp.Models;
using SecureVotingApp.Services;
using SQLitePCL;//controller base
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using OtpNet;

/*
    Claim — Represents one piece of information about a user.
    ClaimsIdentity — Groups claims into an identity.
    ClaimsPrincipal — Represents the user associated with that identity.
*/
namespace SecureVotingApp.Controllers;

[ApiController]//this controller handles API requests
[Route("api/auth")]// URL prefix to access the controller
public class AuthController : ControllerBase
{   
    //[FromBody]	HTTP request body, usually JSON	API registration and login
    //[FromForm]	HTTP form fields in the request body	Traditional HTML forms and file uploads enconded form data
    //[FromQuery]	URL query parameters	Searching and filtering
    //[FromRoute]	Values inside the URL path	Retrieving a user by ID
    private readonly BcryptService _bcryptService;
    private readonly AppDbContext _dbContext;
    //for 2FA
    private readonly TotpService _totpService;
    private readonly PendingRegistrationService _pendingRegistrationService;
    
    private EmailAddressAttribute emailChecker = new EmailAddressAttribute();

    public AuthController(BcryptService bcryptService, AppDbContext dbContext, TotpService totpService, PendingRegistrationService pendingRegistrationService)
    {
        this._bcryptService = bcryptService;
        this._dbContext = dbContext;
        this._totpService = totpService;
        this._pendingRegistrationService = pendingRegistrationService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> UserRegistration([FromBody] UserRegistrationDto request)
    {   
        
        //validate is a email before assigning
        var isEmailNotNull = !string.IsNullOrWhiteSpace(request.Email);
        if(!isEmailNotNull) return BadRequest("Email cannot be Null of Empty");

        //normalise and set email it lower removing whitespaces
        string normalisedEmail = request.Email.Trim().ToLowerInvariant(); 

        //validate email
        bool isValidEmail = emailChecker.IsValid(normalisedEmail);
        if(!isValidEmail) return BadRequest("Valid email required");

        //check that the password is not null
        bool isValidArgon2Hash = !string.IsNullOrWhiteSpace(request.Argon2Hash);
        if(!isValidArgon2Hash) return BadRequest("ArgonHashed Password cannot be null");

        //check salt is not null
        bool isValidSalt = !string.IsNullOrWhiteSpace(request.Argon2Salt);
        if(!isValidSalt) return BadRequest("Salt is required");
        
        
        try
        {
            byte[] argonBytes = Convert.FromBase64String(request.Argon2Hash);
            bool validArgon2Length = argonBytes.Length == 32;
            if(!validArgon2Length) return BadRequest("Invalid Argon2Hashed Password must be 32 bytes");
        }
        catch (FormatException)
        {
            return BadRequest("Invalid Argon2 Encoding");
        }

        try{
            byte[] saltBytes = Convert.FromBase64String(request.Argon2Salt);
            bool validSaltLength = saltBytes.Length == 16;
            if(!validSaltLength) return BadRequest("Salt must be 16 bytes");
        }catch(FormatException)
        {
            return BadRequest("Invalid Salt Encoding");
        }
    
       
        //verify email has not been used
        bool emailExists = await _dbContext.Users.AnyAsync(user => user.Email == normalisedEmail);

        if (emailExists)
        {
            return  Conflict(new { message = "Email already exists"});
        }

        //call the Bcrypt on the Argon2
        string argon2HashedByBycrpt = _bcryptService.HashCredential(request.Argon2Hash);

        // Generate the secret shared with the user's authenticator app.
        string totpSecret = _totpService.GenerateSecret();
        
        // Generate the setup URI that the frontend will display as a QR code.
        string setupUri = _totpService.GenerateTOTPSetupUri(
            normalisedEmail,
            totpSecret
        );

        // Hold registration details temporarily until TOTP setup is verified.
        var pendingRegistration = new PendingRegistration
        {
            Email = normalisedEmail,
            CredentialHash = argon2HashedByBycrpt,
            Argon2Salt = request.Argon2Salt,
            TotpSecret = totpSecret
        };

        // Store the pending registration for 10 minutes.
        string registrationId =
            _pendingRegistrationService.CreatePendingRegistration(pendingRegistration);

        // Return the information needed to display the authenticator QR code.
        return Ok(new
        {
            registrationId,
            setupUri
        });

        /*
        //creating a user object
        var newUser = new User{
            Email = normalisedEmail,
            CredentialHash = argon2HashedByBycrpt,
            Argon2Salt = request.Argon2Salt
        };

        
        _dbContext.Users.Add(newUser); //EF Core to trake newUser as a new record
        //insert into database
        try
        {
            await _dbContext.SaveChangesAsync();//insert into database

        }
        catch (DbUpdateException)
        {
           return Conflict("Registration could not complete");
        }
         */   
       //return Ok();
    }

    [HttpPost("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistration ([FromBody] ConfirmRegistrationDto request)
    {   
        // Retrieve the registration details stored temporarily in memory.
        var pendingUser = _pendingRegistrationService.GetPendingRegistration(
            request.RegistrationId
        );

        // Reject expired or invalid registration references.
        if (pendingUser is null)
        {
            return BadRequest("Registration expired or invalid");
        }

        // Reject missing, incorrectly sized or non-numeric 2FA codes.
        if (string.IsNullOrWhiteSpace(request.Code) ||
            request.Code.Length != 6 ||
            !request.Code.All(char.IsAsciiDigit))
        {
            return Unauthorized("Invalid credentials");
        }

        // Verify the first authenticator code before creating the account.
        bool isValidCode = _totpService.VerifyTOTPCode(
            pendingUser.TotpSecret,
            request.Code
        );

        // Reject an incorrect authenticator code.
        if (!isValidCode)
        {   
            _pendingRegistrationService.RecordFailedAttempt(request.RegistrationId);

            return BadRequest("Invalid authentication code");
        }

        // Encrypt the verified TOTP secret before storing it in SQLite.
        string encryptedSecret = _totpService.EncryptSecret(pendingUser.TotpSecret);

        // Create a permanent account only after successful TOTP verification.
        var newUser = new User
        {
            Email = pendingUser.Email,
            CredentialHash = pendingUser.CredentialHash,
            Argon2Salt = pendingUser.Argon2Salt,
            TotpSecret = encryptedSecret
        };

         _dbContext.Users.Add(newUser); //EF Core to trake newUser as a new record
        //insert into database
        try
        {
            await _dbContext.SaveChangesAsync();//insert into database

        }
        catch (DbUpdateException)
        {
           return Conflict("Registration could not complete");
        }

        // Remove the temporary registration after the account is saved.
        _pendingRegistrationService.RemovePendingRegistration(
            request.RegistrationId
        );

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> UserLogin([FromBody] UserLoginDto request)
    {
        
        //validate is a email before assigning
        var isEmailNotNull = !string.IsNullOrWhiteSpace(request.Email);
        if(!isEmailNotNull) return BadRequest("Email cannot be Null of Empty");

        //normalise and set email it lower removing whitespaces
        string normalisedEmailLogin = request.Email.Trim().ToLowerInvariant(); 

        //validate email
        bool isValidEmail = emailChecker.IsValid(normalisedEmailLogin);
        if(!isValidEmail) return BadRequest("Valid email required");

        //check db if user exist using email if so return the user object 
        User? user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == normalisedEmailLogin);
        if(user is null) return Unauthorized("Invalid credentials");
        
        //check that the password is not null
        bool isValidArgon2Hash = !string.IsNullOrWhiteSpace(request.Argon2Hash);
        if(!isValidArgon2Hash) return BadRequest("ArgonHashed Password cannot be null");

        //check a valid Argon2Hash
         try
        {
            byte[] argonBytes = Convert.FromBase64String(request.Argon2Hash);
            bool validArgon2Length = argonBytes.Length == 32;
            if(!validArgon2Length) return BadRequest("Invalid Argon2Hashed Password must be 32 bytes");
        }
        catch (FormatException)
        {
            return BadRequest("Invalid Argon2 Encoding");
        }

        //to verify credentials 
        bool isValidPassword = _bcryptService.VerifyCredential(request.Argon2Hash, user.CredentialHash);
        if(!isValidPassword) return Unauthorized("Invalid Password");
        
        // Reject missing, incorrectly sized or non-numeric 2FA codes.
        if (string.IsNullOrWhiteSpace(request.Code) ||
            request.Code.Length != 6 ||
            !request.Code.All(char.IsAsciiDigit))
        {
            return Unauthorized("Invalid credentials");
        }

        // Require a configured TOTP secret for every account.
        if (string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            return Unauthorized("Invalid credentials");
        }

       
        // Retrieve the original TOTP secret from its encrypted value.
        string secret = _totpService.DecryptSecret(user.TotpSecret);

        // Verify the six-digit authenticator code.
        bool isValidTotp = _totpService.VerifyTOTPCode(
            secret,//in voting db
            request.Code//the user input 2FA code
        );

        if (!isValidTotp)
        {
            return Unauthorized("Invalid credentials");
        }

        //collection of claims
        var claimId = new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
        var claimEmail = new Claim(ClaimTypes.Email, user.Email);
        var claimColletion = new[] {claimId, claimEmail};
        //ClaimIdentity                                                                 
        var claimIdentity = new ClaimsIdentity(claimColletion, CookieAuthenticationDefaults.AuthenticationScheme);//"cookies"
        //Claim Principle
        var claimPrincipal = new ClaimsPrincipal(claimIdentity);

        //ClaimPrinciple call /creating the cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimPrincipal
        );


        return Ok();
    }

    //for obscurity reason named it details...for users.salt and the post so it does not show in the URI
    [HttpPost("details")]
    public async Task<IActionResult> UserSalt([FromBody] SaltRequestDto request)
    {
         //validate is a email before assigning
        var isEmailNotNull = !string.IsNullOrWhiteSpace(request.Email);
        if(!isEmailNotNull) return BadRequest("Email cannot be Null of Empty");

        //normalise and set email it lower removing whitespaces
        string normalisedEmailLogin = request.Email.Trim().ToLowerInvariant(); 

        //validate email
        bool isValidEmail = emailChecker.IsValid(normalisedEmailLogin);
        if(!isValidEmail) return BadRequest("Valid email required");

        //check db if user exist using email if so return the user object 
        User? user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == normalisedEmailLogin);
        //with out informing a user that the email is incorrect a dummy salt will be generated and sent
        if(user is null) {
            //generate a random number
            byte[] randomBytes = RandomNumberGenerator.GetBytes(16);
            string fakeArgon2Salt = Convert.ToBase64String(randomBytes);
            return Ok(new {details = fakeArgon2Salt});//same signature return  
        }
        return Ok(new {details = user.Argon2Salt});//returns a json
    }

    //logout
    [HttpPost("logout")]
    [Authorize]
    
    public async Task<IActionResult> Logout()
    {   
        //cancelling the cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        return Ok();
    }
 
}    