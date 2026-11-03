using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SecureVotingApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

//@author Moïse register the BlindCredential service with the dependency injection container
builder.Services.AddScoped<IBlindCredential,BlindCredentialClient>();


// @author Moïse register the BlindCredential service with the dependency injection containerregisters the HTTP API client service for calling Voter API endpoints
builder.Services.AddScoped<VoterApiClient>();

// Orchestrates the two-phase voting protocol: requesting credentials via authenticated endpoints
// and submitting anonymous votes via unauthenticated endpoints.
await builder.Build().RunAsync();
