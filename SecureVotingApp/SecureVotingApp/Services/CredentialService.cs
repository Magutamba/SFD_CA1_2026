//@author Moïse
//namespace for grouping services components together
namespace SecureVotingApp.Components.Services;

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
    public Task<CredentialResponse> IssueAsync(
        string userId, string electionId, CredentialRequest request)
    {

        //ensure electionId match
        if(request.ElectionId != electionId)
        {
            throw new InvalidOperationException("The election id does not match");
        }

        //prevent the same user receiving two credentials for the same election
        if (stateStore.HasBeenCredentialed(userId,electionId))
        {
            throw new InvalidOperationException(
                "A credential has already been issued for this election."
                );
        }

        //sign the blinded credential using the server private signing key
        string signedBlindedCredential = BlindSigner(request.BlindedCredential);

        //sign request.BlindCredential using the server private signing key managed
        string signedValue = SignBlindedCredential(request.BlindedCredential);

        //record issuance of credential to prevent double voting after signing
        stateStore.MarkIssued(userId, electionId);
        return Task.FromResult(new CredentialResponse
        {
            ElectionId = electionId,
            SignedBlindedCredential = signedValue,
            Choices = new List<string> {"Yes", "No"},
            CredentialExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        });

    }

    //cryptographic execution of RSA blind signing
    private string BlindSigner(string blindedCredential)
    {
        string SignedBlindedCredential = credentialSigner.SignBlindedCredential(request.BlindedCredential);
    }
}