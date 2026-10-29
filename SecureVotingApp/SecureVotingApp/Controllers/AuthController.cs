// File: AuthController.cs
// @uthor: Jeán Walton
// 29/10/2026

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureVotingApp.Data;
using SecureVotingApp.DTOs;
using SecureVotingApp.Models;
using SecureVotingApp.Services;//controller base

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
    public AuthController(BcryptService bcryptService, AppDbContext dbContext)
    {
        this._bcryptService = bcryptService;
        this._dbContext = dbContext;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> UserRegistration([FromBody] UserRegistrationDto request)
    {   
        //verify email has not been used
        bool emailExists = await _dbContext.Users.AnyAsync(user => user.Email == request.Email);

        if (emailExists)
        {
            return  Conflict(new { message = "Email already exists"});
        }

        //call the Bcrypt on the Argon2
        string argon2HashedByBycrpt = _bcryptService.HashCredential(request.Argon2Hash);

        //creating a user object
        var newUser = new User{
            Email = request.Email,
            CredentialHash = argon2HashedByBycrpt,
            Argon2Salt = request.Argon2Salt
        };

        //add the newUser to the database
        _dbContext.Add(newUser); //EF Core to trake newUser as a new record
        //insert into database
        await _dbContext.SaveChangesAsync();
    
        return Ok(request.Email);
    }

}