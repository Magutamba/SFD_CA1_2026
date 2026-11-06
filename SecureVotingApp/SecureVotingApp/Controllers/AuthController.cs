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
using Microsoft.AspNetCore.RateLimiting;

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
    private readonly ILogger<AuthController> _logger; //Author Aaron Byrne-Logger for storing authentication events such as registering, log-in etc.
    
    private EmailAddressAttribute emailChecker = new EmailAddressAttribute();

        public AuthController(BcryptService bcryptService, AppDbContext dbContext, ILogger<AuthController> logger)
    {
        this._bcryptService = bcryptService;
        this._dbContext = dbContext;
        this._logger = logger; //Author Aaron Byrne-Stores logger passed in constructor for use in this controller
    }
    
    [HttpPost("register")]
    [EnableRateLimiting("StrictPolicy")]
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
            
        //Author Aaron Byrne-Logging a new user registration
        _logger.LogInformation(
            "{UserID} has had their {EventType}",
            newUser.Id,
            "Registration successful"
            );
        return Ok();
    }


    [HttpPost("login")]
    [EnableRateLimiting("StrictPolicy")]
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
        if(user is null)
        {
            //Author Aaron Byrne-Logging a failed login when the inputted credentials are invalid
            _logger.LogWarning(
                "{EventType} occured because {Reason}",
                "Login has failed",
                "Invalid Credentials"
            );
            return Unauthorized("Invalid credentials");
        }
        
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
        if(!isValidPassword)
            //Author Aaron Byrne-Logging a failed login when password is incorrect
        {
            _logger.LogWarning(
                "{EventType} occured because {Reason}",
                "Login has failed",
                "Invalid Credentials"
                );
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

        //Author Aaron Byrne-Logging a successful login after user has signed in
        _logger.LogInformation(
            "User {UserId} {EventType}",
            user.Id,
            "Login has succeeded"
            
        );
        
        return Ok();
    }

    //for obscurity reason named it details...for users.salt and the post so it does not show in the URI
    
    [HttpPost("details")]
    [EnableRateLimiting("StrictPolicy")]
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
    [EnableRateLimiting("StandardPolicy")]
    
    public async Task<IActionResult> Logout()
    {   
        //cancelling the cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        return Ok();
    }
 
}    