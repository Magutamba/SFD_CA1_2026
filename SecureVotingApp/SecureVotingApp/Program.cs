using SecureVotingApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add API Controllers - None currently exist but will be needed for future
builder.Services.AddControllers();
//@author Moïse add singleton service for signing credentials with RSA private key, PEM file path included
builder.Services.AddSingleton<CredentialSigner>(_ => new CredentialSigner("Keys/voting_signing_key.pem"));
//@author Moïse register state with state store to track issued credentials per voter session
builder.Services.AddSingleton<CredentialStateStore>();
//@author Moïse 
builder.Services.AddScoped<CredentialService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();

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