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

//service for calling voter API endpoints for requesting credentials and ballots
public class VoterApiClient
{
    //private readlonly field for storing the injected HTTP client instance used for API calls
    private readonly HttpClient httpClient;
    //private readlonly field for storing the injected client service for cryptographically blinding the voter in client
    private readonly IBlindCredentialClient blindVoterClient;
    
    //constructor for VoterApiClient class, accepts dependencies via ASP.NET Core dpendency injection
    public VoterApiClient(HttpClient httpClient)
    {
        //assign the injected HTTP client instance to the private field for use in API calls
        this.httpClient = httpClient;
        //assign injected blind credential service to the class field for use in blinding credentials before sending to server
        this.blindVoterClient = blindVoterClient;
    }

    //method for requesting credentials and ballot from the server for voter to cast their vote
    public async Task<VotingSession> RequestBallotAsync(string electionId)
    {
        //genereate a random credential and blind it in client
        BlindCredentialRequest blindRequest = blindVoterClient.CreateBlindedCredential(electionId);

        //send the blinded credential to the server and request a signed credential and ballot
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Vote/electionId/credential");

        //the login cookie is included so server can verify voter logged in
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        //wrap the blinded credential and electionId payload in a CredentialRequest object and serialize to JSON for the request body
        request.Content = JsonContent.Create(
            new AnonymousVoteSubmit
            {
                ElectionId = electionId,
                BlindedCredential = blindRequest.BlindedCredential
            }
        );
        //send the request to the server and wait for the response signed credential and ballot
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        //ensure the response indicates success
        response.EnsureSuccessStatusCode();

        //deserialize the response content to a CredentialResponse object which contains signed credential and ballot
        CredentialResponse credentialResponse =
            await response.Content.ReadFromJsonAsync<CredentialResponse>() 
            ?? throw new InvalidOperationException(
                "The credential response is empty."
            );

        //remove the blinding from the signed credential
        string anonymousCredential = blindVoterClient.UnblindCredential(
            // pass the signed ballot creddential
            credentialResponse.SignedBlindedCredential,
            blindRequest.State
        );
        return new VotingSession
        {
            
        }

        {
            ElectionId = credentialResponse.ElectionId,
            AnonymousCredential = anonymousCredential,
            Candidates = credentialResponse.Candidates,
            ExpiresAt = credentialResponse.ExpiresAt
        };
    }

    public async Task<VoteReceipt> SubmitVoteAsync(
            VotingSession session,
            string candidateId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/votes");

        // Do not send the login cookie with the anonymous vote.
        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Omit
        );

        request.Content = JsonContent.Create(
            new AnonymousVoteSubmit
            {
                ElectionId = session.ElectionId,
                AnonymousCredential =
                    session.AnonymousCredential,
                CandidateId = candidateId
            }
        );

        using HttpResponseMessage response =
            await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<
            VoteReceipt>()
            ?? throw new InvalidOperationException(
                "The vote response was empty."
            );
    }
}



    }
}
