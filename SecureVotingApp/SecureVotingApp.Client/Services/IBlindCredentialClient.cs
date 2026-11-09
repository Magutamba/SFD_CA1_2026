//@author Moïse
//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//define the client side Blinding operations
public interface IBlindCredentialClient
{
    // task makes the method asynchronous and allows for non-blocking operations, improving responsiveness in client applications
    //creates a random credential and blinds it in client
    Task<BlindCredentialRequest> CreateBlindedCredentialAsync(string electionId);

    //remove the blinding from the signed credential
    Task<string> UnblindCredentialAsync(string signedBlindedCredential, BlindState state);
}