//@author Moïse
//provide JSON helper extension methods for HtppClient(GETFromAsyncJson)
//symplify JSON serialization and deserialization when calling API endpoints 
using System.Net.Http.Json;
//imports types for HTTP requests specific for Blazor WebAssembly apps, including extension methods for sending and receiving JSON data
using Microsoft.AspNetCore.Components.WebAssembly.Http;
// * ADDED: imports the client-side request and response contracts.
using SecureVotingApp.Client.Contracts;
//provides access to identity claims to retrieve the logged in userId
using System.Security.Claims;
//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//service for calling voter API endpoints for requesting credentials and ballots
public class VoterApiClient
{
    //private readlonly field for storing the injected HTTP client instance used for API calls
    private readonly HttpClient httpClient;
    //private readlonly field for storing the injected client service for cryptographically blinding the voter in client
    private readonly IBlindCredentialClient blindVoterClient;
    //***symplify application, but allows for future extension
    private const string defaultElectionId = "election-2026";

    //constructor for VoterApiClient class, accepts dependencies via ASP.NET Core dpendency injection
    public VoterApiClient(HttpClient httpClient, IBlindCredentialClient blindVoterClient)
    {
        //assign the injected HTTP client instance to the private field for use in API calls
        this.httpClient = httpClient;
        //assign injected blind credential service to the class field for use in blinding credentials before sending to server
        this.blindVoterClient = blindVoterClient;
    }
    //***overload the method and allowing callers to omit the electionId and use the default electionId
    public Task<VoteSession> RequestBallotAsync()
    {
        return RequestBallotAsync(defaultElectionId);
    }

    //method for requesting credentials and ballot from the server for voter to cast their vote
    public async Task<VoteSession> RequestBallotAsync(string electionId)
    {
        //safe guard to ensure electionId is not null or empty use  the defaultElectionId if it is
        string currentElectionId = string.IsNullOrWhiteSpace(electionId) ? defaultElectionId : electionId;
        //generate a blinded credential request locally
        BlindCredentialRequest blindRequest = await blindVoterClient.CreateBlindedCredentialAsync(currentElectionId);

        //generate recovery material and encrypted package locally
        var client = (BlindCredentialClient)blindVoterClient;
        RecoveryMaterial recoveryMaterial = client.CreateRecoveryMaterial(blindRequest.State, currentElectionId);


        //send the blinded credential to the server and request a signed credential and ballot
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/elections/{currentElectionId}/credential");

        //the login cookie is included so server can verify voter logged in
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        //wrap the blinded credential and electionId payload in a CredentialRequest object and serialize to JSON for the request body
        request.Content = JsonContent.Create(new CredentialRequest
        {
            ElectionId = currentElectionId,//*** pass the safeguarded electionId
            BlindedCredential = blindRequest.BlindedCredential,
            RecoveryPackage = recoveryMaterial.RecoveryPackage

        });

        //send the request to the server and wait for the response signed credential and ballot
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        //handle unauthorized voter request
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("You must be logged in to request credentials and a ballot.");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("You are not eligible to request credentials or have already been issued a credential.");
        }
        //handle conflict response from server if voter has already been issued a credential for the election
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("You have already been issued a credential for this election.");
        }
        //ensure the response indicates success
        response.EnsureSuccessStatusCode();

        //deserialize the response content to a CredentialResponse object which contains signed credential and ballot
        CredentialResponse credentialResponse =
            await response.Content.ReadFromJsonAsync<CredentialResponse>()
            ?? throw new InvalidOperationException(
                "The credential response is empty."
            );

        //remove the blinding from the signed credential
        string anonymousCredential = await blindVoterClient.UnblindCredentialAsync(
            // pass the signed ballot creddential and blinding state to unblind method and get signed credential for anonimity whilst voting
            credentialResponse.SignedBlindedCredential,
            blindRequest.State
        );
        //return a Vote Session object containin electionId, signed credential 
        return new VoteSession
        {
            ElectionId = credentialResponse.ElectionId,
            AnonymousCredential = anonymousCredential,
            MessageRandomizer = blindRequest.State.MessageRandomizer,
            VoteChoice = string.Empty,
            RecoveryKey = recoveryMaterial.RecoveryKey
        };

    }

    //method for submitting vote to the server using anonymous signed credentials, electionId and asVoted
    public async Task<VoteReceipt> SubmitVoteAsync(
        VoteSession session,
        string voteChoice)
    {

        //POST request target votes endpoint
        //using  var ensures memory and headers are automatically disposed once executed
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/vote");

        //login cookie not needed for submission of vote
        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Omit
        );

        //wrap signed credential, electionId, Candidate choice, vote choice, as voted token/expiry in AnonymousVoteSubmit object and serialize to JSON request body
        request.Content = JsonContent.Create(
            new AnonymousVoteSubmit
            {
                ElectionId = session.ElectionId,
                AnonymousCredential = session.AnonymousCredential,
                MessageRandomizer = session.MessageRandomizer,
                VoteChoice = voteChoice,
            }

        );

        //send the request to the server and wait for the response containin vote receipt
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        //ensure the response indicates success
        response.EnsureSuccessStatusCode();

        // deserialize the response content to a VoteReceipt object which contains confirmation of vote submission
        VoteReceipt voteReceipt = await response.Content.ReadFromJsonAsync<VoteReceipt>()
            ?? throw new InvalidOperationException("The vote response was empty."
            );

        //return confirmation of accepted vote 

        return voteReceipt;

    }

    //method for requesting original credential recovery using key
    public async Task<VoteSession?> RecoverBallotAsync(string electionId, string recoveryKey)
    {
        //safe guard to ensure electionId is not null or empty use  the defaultElectionId if it is
        string currentElectionId = string.IsNullOrWhiteSpace(electionId) ? defaultElectionId : electionId;

        //send the blinded credential to the server and request a signed credential and ballot
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/elections/{currentElectionId}/credential");

        //the login cookie is included so server can verify voter logged in
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        //wrap the blinded credential and electionId payload in a CredentialRequest object and serialize to JSON for the request body
        request.Content = JsonContent.Create(new CredentialRequest
        {
            ElectionId = currentElectionId,//*** pass the safeguarded electionId
            BlindedCredential = string.Empty

        });

        //send the request to the server and wait for the response signed credential and ballot
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        //handle unauthorized voter request
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("You must be logged in to request credentials and a ballot.");
        }
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("You are not eligible to request credentials or have already been issued a credential.");
        }
        //handle conflict response from server if voter has already been issued a credential for the election
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("You have already been issued a credential for this election.");
        }
        //ensure the response indicates success
        response.EnsureSuccessStatusCode();

        //deserialize the response content to a CredentialResponse object which contains signed credential and ballot
        CredentialResponse result =
            await response.Content.ReadFromJsonAsync<CredentialResponse>()
            ?? throw new InvalidOperationException(
                "Recovery response is empty"
            );
        //decryption happens exclusively in the client
        var client = (BlindCredentialClient)blindVoterClient;
        //restore the blinding state
        BlindState restoredState = client.RestoreRecoveryPackage(result.RecoveryPackage,recoveryKey, currentElectionId);
        //remove the blinding from the signed credential
        string anonymousCredential = await blindVoterClient.UnblindCredentialAsync(
            // pass the signed ballot creddential and blinding state to unblind method and get signed credential for anonimity whilst voting
            result.SignedBlindedCredential,
            restoredState
        );
        //return a Vote Session object containin electionId, signed credential 
        return new VoteSession
        {
            ElectionId = result.ElectionId,
            AnonymousCredential = anonymousCredential,
            MessageRandomizer = restoredState.MessageRandomizer,
            VoteChoice = string.Empty,
            RecoveryKey = recoveryKey
        };
    }

}
