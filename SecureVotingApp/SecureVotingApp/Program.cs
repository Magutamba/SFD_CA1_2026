using SecureVotingApp.Components;
using SecureVotingApp.Data;
using SecureVotingApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

//registering Dependency Injection
var builder = WebApplication.CreateBuilder(args);
//back end (DI) registery (Jeán)
builder.Services.AddScoped<BcryptService>(); //hashing the argon2 
//configure which Database (EFC) will use all configurations and passed to the super class of the AppDbContext class
builder.Services.AddDbContext<AppDbContext>(options =>
{   
    //connect configuration to EF Core access/edit the sql database (EFC)
    options.UseSqlite(builder.Configuration.GetConnectionString("SQLite_Connection"));
});

//registers functionality with dependency injection.
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


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add API Controllers - None currently exist but will be needed for future
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

// temp remove until frontend is connectd//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

//cookies 
app.UseAuthentication();//app.Use... adds middleware to the HTTP request pipeline
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


//this is for AuthControllerIntegrationTests.cs
public partial class Program
{
    
}
/*Note:
    builder.Services.Add... registers services and configures functionality.
    app.Use... adds middleware that participates in processing HTTP requests and responses.
    app.Map... defines which endpoints can handle requests.
 */