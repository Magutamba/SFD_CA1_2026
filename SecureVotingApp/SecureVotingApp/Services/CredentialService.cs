//@author Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System;
// directives to use for using collections and lists
using System.Collections.Generic;
//directives to support asynchronous programming and tasks
using System.Threading.Tasks;
//directives to use the contracts
using SecureVotingApp.Client.Contracts;
//rouping services components together
namespace SecureVotingApp.Services;

//store information about the credential state of the current voting session
public class CredentialService
{
    //server
    private readonly CredentialSigner credentialSigner;
    //state store to persist credential issued per user session
    private readonly CredentialStateStore stateStore;

    //constructor to inject the state store dependency
    public CredentialService(CredentialSigner credentialSigner, CredentialStateStore stateStore)
    {
        this.credentialSigner = credentialSigner;
        this.stateStore = stateStore;
    }

    //validates the credential request and issues aa signed blinded credential if the user is eligible
    public async Task<CredentialResponse> IssueAsync(
        int userId, string electionId, CredentialRequest request)
    {

        //if userId is invalid or electionI
        if(userId<=0)
        {
            throw new InvalidOperationException("Invalid userId");
        }

        if(string.IsNullOrWhiteSpace(electionId))
        {

            throw new InvalidOperationException("Invalid electionId");
        }
        //ensure electionId match
        if(request.ElectionId != electionId)
        {
            throw new InvalidOperationException("The election id does not match");
        }

        if (string.IsNullOrWhiteSpace(request.BlindedCredential))
        {
            throw new InvalidOperationException("Blinded credential is required");
        }

        //reserve the credential for ths user before signing to prevent conflicts with concurrent requests
        bool reserved = await stateStore.TryMarkIssuedAsync(userId, electionId);
        if (!reserved)
        {
            throw new InvalidOperationException("Could not reserve credentials");

        }

        //sign the blinded credential using the server private signing key
        string signedValue = credentialSigner.SignBlindedCredential(request.BlindedCredential);
        
        return new CredentialResponse
        {
            ElectionId = electionId,
            SignedBlindedCredential = signedValue,
            Choices = new List<string> {"Yes", "No"},
            CredentialExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

    }
}