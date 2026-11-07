// File: AuthControllerIntegrationTests.cs
// @uthor: Jeán Walton
// 30/10/2026

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Bson;
using OtpNet;
using Xunit;

public class AuthControllerIntegrationTests : IDisposable
{
    private readonly AuthTestFactory _authTestFactory;
    private readonly HttpClient _httpClient;
    public AuthControllerIntegrationTests()
    {
        _authTestFactory = new AuthTestFactory();
        _authTestFactory.InitialiseDatabase();

        _httpClient = _authTestFactory.CreateClient(
            new WebApplicationFactoryClientOptions//this is https for cookies (CookieSecurePolicy.Always)
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    //xUnit calls this method automatically after each test, so every test releases it resources
    public void Dispose()
    {
        _httpClient.Dispose();
        _authTestFactory.Dispose();
    }

    //sending valid registration details 
    [Fact]
    public async Task RegisterUser_withValidDetails_ReturnsOK()
    {   

        //Arrange
        var registration = new
        {
            Email = "test@example.com", 
            Argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray()),
            Argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray())
        };

        //Act
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Registration response: {responseBody}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Check that registration returns the information needed for 2FA setup.
        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync()
        );

        string? registrationId = json.RootElement
            .GetProperty("registrationId")
            .GetString();

        string? setupUri = json.RootElement
            .GetProperty("setupUri")
            .GetString();
        //example of the return type
        //secret=ABC123...&issuer=SecureVotingApp&algorithm=SHA1&digits=6&period=30 (used for the QR Code)

        Assert.False(string.IsNullOrWhiteSpace(registrationId));
        Assert.StartsWith("otpauth://totp/", setupUri);

    
    }

    [Fact]
    public async Task RegisterUser_withDuplicateEmail_ReturnsConflict()
    {   
        //Arrange
        var registration = new
        {
            Email = "test@example.com", 
            Argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray()),
            Argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray())
        };

         //Act
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);
       
        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);//first registery

         // Confirm the first registration before checking for a duplicate email.
        using var registrationJson = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        string registrationId = registrationJson.RootElement
            .GetProperty("registrationId").GetString()!;
        string setupUri = registrationJson.RootElement
            .GetProperty("setupUri").GetString()!;

        string secret = setupUri.Split("secret=")[1].Split('&')[0];
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        var confirmation = new
        {
            RegistrationId = registrationId,
            Code = totp.ComputeTotp()
        };

        var responseConfirmation = await _httpClient.PostAsJsonAsync(
            "/api/auth/confirm-registration", confirmation);
        Assert.Equal(HttpStatusCode.OK, responseConfirmation.StatusCode);

         //Act
        var responseDuplicate = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);

        //Assert    
        Assert.Equal(HttpStatusCode.Conflict, responseDuplicate.StatusCode);//second registery 
      
    }

    [Fact]
    public async Task Login_withValidCredentials_ReturnOk()
    {   
        //Arrange
        var email = "test@example.com";
        var argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
        var argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());

        //Arrange
         var registration = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            Argon2Salt = argon2Salt
        };

        //Act (register new user)
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //Reading the registration ID and TOTP setup URI
        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync()
        );

        //extract the properties from the return json file
        String registrationId = json.RootElement
            .GetProperty("registrationId")
            .GetString()!;

        String setupUri = json.RootElement
        .GetProperty("setupUri")
        .GetString()!;

        //extracting the TOTP secrect from the setup URI
        String secret = setupUri.Split("secret=")[1].Split('&')[0];

        //create an authenticator using the shared secret
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        //generate a valid code to confirm registration
        string registrationCode = totp.ComputeTotp();

        //confirmation json to send to backend "confirm-registration"
        var confirmation = new
        {
            RegistrationId = registrationId,
            Code = registrationCode
        };

        //confirm 2FA and save the temp user details to the sqlite database
        var responseConfirmation = await _httpClient.PostAsJsonAsync(
            "api/auth/confirm-registration", confirmation);

        //Assert
        Assert.Equal(HttpStatusCode.OK, responseConfirmation.StatusCode);

        //we cant reuse the same code
        string loginCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(30));

        //Arrange 
        var login = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            code = loginCode
        };

        //Login 
        var responseLogin = await _httpClient.PostAsJsonAsync(
            "/api/auth/login", login);

        //Assert   
        Assert.Equal(HttpStatusCode.OK, responseLogin.StatusCode);

    }

    [Fact]
    public async Task Logout_withAuthenticatedUser_ReturnOk()
    {
        //Arrange
        var email = "test@example.com";
        var argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
        var argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());

        //Arrange
         var registration = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            Argon2Salt = argon2Salt
        };

        //Act (register new user)
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //Reading the registration ID and TOTP setup URI
        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync()
        );

        //extract the properties from the return json file
        String registrationId = json.RootElement
            .GetProperty("registrationId")
            .GetString()!;

        String setupUri = json.RootElement
        .GetProperty("setupUri")
        .GetString()!;

        //extracting the TOTP secrect from the setup URI
        String secret = setupUri.Split("secret=")[1].Split('&')[0];

        //create an authenticator using the shared secret
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        //generate a valid code to confirm registration
        string registrationCode = totp.ComputeTotp();

        //confirmation json to send to backend "confirm-registration"
        var confirmation = new
        {
            RegistrationId = registrationId,
            Code = registrationCode
        };

        //confirm 2FA and save the temp user details to the sqlite database
        var responseConfirmation = await _httpClient.PostAsJsonAsync(
            "api/auth/confirm-registration", confirmation);

        //Assert
        Assert.Equal(HttpStatusCode.OK, responseConfirmation.StatusCode);

        //we cant reuse the same code
        string loginCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(30));

        //Arrange 
        var login = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            code = loginCode
        };

        //Login 
        var responseLogin = await _httpClient.PostAsJsonAsync(
            "/api/auth/login", login);

        //Assert   
        Assert.Equal(HttpStatusCode.OK, responseLogin.StatusCode);

        //Act
        // logout does not require a body just authenication
        var responseLogout = await _httpClient.PostAsync(
            "/api/auth/logout", null);

        //Assert
        Assert.Equal(HttpStatusCode.OK, responseLogout.StatusCode);

         //verify logout clears the cookie
        Assert.True(
            responseLogout.Headers.TryGetValues("Set-Cookie", out var cookies)
        );

        var logoutCookie = string.Join("; ", cookies!);

        Assert.Contains(".AspNetCore.Cookies=", logoutCookie);
        Assert.Contains("expires=", logoutCookie, StringComparison.OrdinalIgnoreCase);

        var secondLogoutResponse = await _httpClient.PostAsync(
            "/api/auth/logout", null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            secondLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorised()
    {
         //Arrange
        var email = "test@example.com";
        var argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
        var argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());

        //Arrange
         var registration = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            Argon2Salt = argon2Salt
        };

        //Act (register new user)
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //Reading the registration ID and TOTP setup URI
        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync()
        );

        //extract the properties from the return json file
        String registrationId = json.RootElement
            .GetProperty("registrationId")
            .GetString()!;

        String setupUri = json.RootElement
        .GetProperty("setupUri")
        .GetString()!;

        //extracting the TOTP secrect from the setup URI
        String secret = setupUri.Split("secret=")[1].Split('&')[0];

        //create an authenticator using the shared secret
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        //generate a valid code to confirm registration
        string registrationCode = totp.ComputeTotp();

        //confirmation json to send to backend "confirm-registration"
        var confirmation = new
        {
            RegistrationId = registrationId,
            Code = registrationCode
        };

        //confirm 2FA and save the temp user details to the sqlite database
        var responseConfirmation = await _httpClient.PostAsJsonAsync(
            "api/auth/confirm-registration", confirmation);

        //Assert
        Assert.Equal(HttpStatusCode.OK, responseConfirmation.StatusCode);

        //we cant reuse the same code
        string loginCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(30));

        //Arrange 
        var login = new
        {
            Email = email, 
            Argon2Hash = "Y2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2M=",  //incorrect password argon2Hash,
            code = loginCode
        };

        //Login 
        var responseLogin = await _httpClient.PostAsJsonAsync(
            "/api/auth/login", login);

        //Assert
        Assert.Equal(HttpStatusCode.Unauthorized, responseLogin.StatusCode);
       
    }

    [Fact]
    public async Task Login_WithIncorrectEmail_ReturnsUnauthorised()
    {
        var email = "test@example.com";
        var argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
        var argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());

        //Arrange
         var registration = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            Argon2Salt = argon2Salt
        };

        //Act (register)
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);

        //Arrange 
        var login = new
        {
            Email = "wrong@email.com", //incorrect email
            Argon2Hash = argon2Hash,
            Code = "123456"
        };

        //Login 
        var responseLogin = await _httpClient.PostAsJsonAsync(
            "/api/auth/login", login);

        //Assert
         Assert.Equal(HttpStatusCode.Unauthorized, responseLogin.StatusCode);
  
    }

    [Fact]
    public async Task Details_withRegisterdEmail_ReturnsCorrectSalt()
    {
        var email = "test@example.com";
        var argon2Hash = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
        var argon2Salt = Convert.ToBase64String(
                Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());

        //Arrange
         var registration = new
        {
            Email = email, 
            Argon2Hash = argon2Hash,
            Argon2Salt = argon2Salt
        };

         //Act (register)
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Confirm 2FA so the user's real salt is stored in SQLite.
        using var registrationJson = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        string registrationId = registrationJson.RootElement
            .GetProperty("registrationId").GetString()!;
        string setupUri = registrationJson.RootElement
            .GetProperty("setupUri").GetString()!;

        string secret = setupUri.Split("secret=")[1].Split('&')[0];
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        var confirmation = new
        {
            RegistrationId = registrationId,
            Code = totp.ComputeTotp()
        };

        var responseConfirmation = await _httpClient.PostAsJsonAsync(
            "/api/auth/confirm-registration", confirmation);
        Assert.Equal(HttpStatusCode.OK, responseConfirmation.StatusCode);

        //Arrange 
        var details = new
        {
            Email = email, 
            
          
        };

         
        var responseDetails = await _httpClient.PostAsJsonAsync(
            "/api/auth/details", details);

        //Assert
        Assert.Equal(HttpStatusCode.OK, responseDetails.StatusCode);

        //verify frontend is receiving the json with the correct salt
        using var json = JsonDocument.Parse(
            await responseDetails.Content.ReadAsStreamAsync()
        );

        var returnedSalt = json.RootElement
            .GetProperty("details")
            .GetString();
        
        Assert.Equal(argon2Salt, returnedSalt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Details_WithUnknownEmail_ReturnsDummySalt()
    {   
        //Arrange
        var email = "test@example.com";

        var details = new
        {
            Email = email, 
              
        };

        //Act  
        var responseDetails = await _httpClient.PostAsJsonAsync(
            "/api/auth/details", details);

        //Assert
        Assert.Equal(HttpStatusCode.OK, responseDetails.StatusCode);

        //Act
        //verify frontend is receiving the json with a salt value (fake)
        using var json = JsonDocument.Parse(
            await responseDetails.Content.ReadAsStreamAsync()
        );

        var returnedSalt = json.RootElement
        .GetProperty("details")
        .GetString();

        var saltBytes = Convert.FromBase64String(returnedSalt!);
        
        //Assert
        Assert.Equal(16, saltBytes.Length);
    }

    [Fact]
    public async Task Logout_withoutAuthentication_ReturnsUnauthorised()
    {
        //Act requesting logout without logging in 
        var response = await _httpClient.PostAsync( 
            "/api/auth/logout", null);
        
        //assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}