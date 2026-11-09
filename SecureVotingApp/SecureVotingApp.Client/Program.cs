using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SecureVotingApp.Client;
using SecureVotingApp.Client.Contracts;
using SecureVotingApp.Client.Services;
using Blazored.SessionStorage;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//@author Moïse register the Api client used by Vote
builder.Services.AddScoped<VoterApiClient>();
//@author Moïse register client side blind credential implementation
builder.Services.AddScoped<IBlindCredentialClient, BlindCredentialClient>();
//@author Moïse register browser session storage
builder.Services.AddBlazoredSessionStorage();
// Shared authentication state with server (Lorenzo)
// Allows for easy checking of authentication state within blazor pages without need for querying api endpoints
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

await builder.Build().RunAsync();
