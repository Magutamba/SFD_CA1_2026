using SecureVotingApp.Components;
using SecureVotingApp.Data;
using SecureVotingApp.Services;
using Microsoft.EntityFrameworkCore;

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(SecureVotingApp.Client._Imports).Assembly
    );
app.Run();