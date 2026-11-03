//@author Moïse
//namespace for grouping services components together
namespace SecureVotingApp.Services;

// state store to keep track of credential state during vote session
//temporary replace with database SQLlite
public class CredentialStateStore
{
    //temporary in-memory store to track issued credentials per voter session
    private readonly HashSet<string> issuedCredentials = new ();

    //check if voter has already been issued a credential for the given election 
    public bool HasBeenCredentialed(string userId,string electionId)
    {
        return issuedCredentials.Contains(userId + ":" + electionId);
    }

    //mark that a credential as been issued and store to prevent double voting
    public void MarkIssued(string userId, string electionId)
    {
        issuedCredentials.Add(userId + ":" + electionId);
    }
}