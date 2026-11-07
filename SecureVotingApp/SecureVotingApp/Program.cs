using SecureVotingApp.Components;
using SecureVotingApp.Data;
using SecureVotingApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Formatting.Compact;//Aaron Byrne-Allows for logs to be displays as JSON rather than plaintext
using Serilog.Events;//Aaron Byrne-Allows for logging levels to be set, testing showed that over 1000 lines in the log were being created, which makes reading the information we need difficult

//registering Dependency Injection
var builder = WebApplication.CreateBuilder(args);

//Author Aaron Byrne- Registering Serilog to be our logger and sending log messages to the terminal in IDE https://github.com/serilog/serilog-aspnetcore#serilogaspnetcore--- 
builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .MinimumLevel.Information()//Author Aaron Byrne-Ensures debug messages do not get saved to logs, but errors, info and warnings do
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)//Author Aaron Byrne-Only records errors and warnings from Micorsoft framework code
        .MinimumLevel.Override("System", LogEventLevel.Warning)//Author Aaron Byrne-Ensures that logging from background .NET components is reduced
        .WriteTo.Console()
        .WriteTo.File(//Author Aaron Byrne-Saves logs to a file as JSON 
        new RenderedCompactJsonFormatter(),//Author Aaron Byrne-Rendered allows for UserID and EventType to actually show up within the logs
        "appLogs/logs.json",//Author Aaron Byrne-Saves logs within a JSON folder
        rollingInterval: RollingInterval.Day);//Author Aaron Byrne-This will create a new log file each day
});

//back end (DI) registery (Jeán Walton)
builder.Services.AddScoped<BcryptService>(); //hashing the argon2 
builder.Services.AddScoped<TotpService>(); // (Lorenzo) was missing from my build - needed to be readded
builder.Services.AddDataProtection(); // (Lorenzo) same as above
//configure which Database (EFC) will use all configurations and passed to the super class of the AppDbContext class
builder.Services.AddDbContext<AppDbContext>(options =>
{   
    //connect configuration to EF Core access/edit the sql database (EFC)
    options.UseSqlite(builder.Configuration.GetConnectionString("SQLite_Connection"));
});

//registers functionality with dependency injection. (Jeán Walton)
builder.Services                //exactly the same as .AddAuthentication("Cookies") ..just safer
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)//default authenication scheme
    .AddCookie(options => {//register cookie and set security 
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;//authentican cookie must be sent over HTTPS
        options.Cookie.SameSite = SameSiteMode.Strict; //to mitigate against cross-site attacks
        //set time without automatic renewal 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = false;//disable automatic renewal
    });

// Add rate limiting service to build (Lorenzo)
// Contains strict and standard rules to be associated with API controllers to limit rate at which requests can be sent by client
// Strict rules are explicitly for anonymous requests - where no user has been authorized (e.g., Login)
// Standard rules are explicitly for non-anonymous requests - where an authorized user is making requests (e.g.,Voting)
builder.Services.AddCustomRateLimiter();

builder.Services.AddCascadingAuthenticationState(); // Shared authentication state with client (Lorenzo)
//@author Moïse create path for storing RSA private key
string privateKeyPath = Path.Combine(builder.Environment.ContentRootPath,"Keys", "voting_signing_key.pem");
//@author Moïse add singleton service for signing credentials with RSA private key, PEM file path included
//might need to change the path
builder.Services.AddSingleton<CredentialSigner>(_ => new CredentialSigner(privateKeyPath));
//@author Moïse db state with state store to track issued credentials per voter session
builder.Services.AddScoped<CredentialStateStore>();
//@author Moïse 
builder.Services.AddScoped<CredentialService>();
// Temporarily store pending registrations until TOTP setup is confirmed.(Jeán Walton)
builder.Services.AddMemoryCache();

// Manage temporary registrations until TOTP enrollment is verified.(Jeán Walton)
builder.Services.AddSingleton<PendingRegistrationService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents() // Allows blazor client to render page and keep sensitive information away from server or transmission over network
    .AddAuthenticationStateSerialization(); // Shared authentication state with client

// Add API Controllers - (Jeán Walton)
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();//Controllers

// Show Blazor error pages without overwriting API error responses.(Jeán Walton)
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseStatusCodePagesWithReExecute(
            "/not-found",
            createScopeForStatusCodePages: true);
    });

app.UseHttpsRedirection();

//cookies (Jeán Walton)
app.UseAuthentication();//app.Use... adds middleware to the HTTP request pipeline
app.UseRateLimiter(); // Rate Limiter middleware requires that authentication middleware service is active - must come after to ensure working rate limiter association to IP / Use (Lorenzo)
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(SecureVotingApp.Client._Imports).Assembly
    );
app.Run();


//this is for AuthControllerIntegrationTests.cs(Jeán Walton)
public partial class Program
{
    
}
/*Note:
    builder.Services.Add... registers services and configures functionality.
    app.Use... adds middleware that participates in processing HTTP requests and responses.
    app.Map... defines which endpoints can handle requests.
 */