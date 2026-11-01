//@author Moïse
//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//define the client side Blinding operations
public interface IBlindCredentialClient
{
    //creates a random credential and blinds it in client
    BlindCredentialRequest CreateBlindedCredential(string electionId);

    //remove the blinding from the signed credential
    string unblindCredential(string signedBlindedCredential, BlindState state);
}