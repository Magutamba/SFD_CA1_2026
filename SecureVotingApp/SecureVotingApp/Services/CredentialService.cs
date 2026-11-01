//@author Moïse
//namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//store information about the credential state of the current voting session
public class CredentialService
{   
    //server
    private readonly CredentialSigner credentialSigner;
    //state store to persist credential issued per user session
    private readonly CredentialStateStore stateStore;

    //constructor to inject the state store dependency
    public CredentialService(CredentialSigner credentialSigner,CredentialStateStore stateStore)
    {
        this.credentialSigner = credentialSigner;
        this.stateStore = stateStore;
    }

    //validates the credential request and issues aa signed blinded credential if the user is eligible
    public Task<CredentialResponse> IssueAsync(
        string userId,string electionId, CredentialRequest request)
    {
        
    
    //prevent the same user receiving two credentials for the same election
    if (stateStore.HasBeenCredentialed(electionId)){
        throw new InvlidOperationException(
            "A credential has already been issued for this election."
            );
    }

    //mark credential as issued for this useer
    stateStore.MarkIssued(userId,electionId);

    //sign request.BlindCredential using the server private signing key managed
    string signedValue = SignBlindedCredential(request.BlindedCredential);

    return Task.FromResult(new CredentialResponse
    {
        ElectionId = electionId,
        SignedBlindedCredential = signedValue
    });
    }

    //cryptographic execution of RSA blind signing
    private string BlindSigner(string blindedCredential)
    {
        string SignedBlindedCredential = credentialSigner.SignBlindedCredential(request.BlindedCredential);
    }
    }
}