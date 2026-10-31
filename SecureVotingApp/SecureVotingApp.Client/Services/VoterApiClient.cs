//@author Moïse 
//provide JSON helper extension methods for HtppClient(GETFromAsyncJson)
//symplify JSON serialization and deserialization when calling API endpoints 
using System.Net.Http.Json;
//imports types for HTTP requests specific for Blazor WebAssembly apps, including extension methods for sending and receiving JSON data
using Microsoft.AspNetCore.Components.WebAssembly.Http;
//logical namespace to group components contracts together prevents class name collisions
using SecureVotingApp.Components.Contracts;

//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

