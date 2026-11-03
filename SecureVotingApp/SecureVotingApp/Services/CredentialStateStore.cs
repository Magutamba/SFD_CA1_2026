//@author Moïse
//namespace for grouping services components together
namespace SecureVotingApp.Services;

// state store to keep track of credential state during vote session
public class CredentialStateStore
{
    //store issued credentials to prevent doubl voting
    private readonly AppDbContext dbContext;

    public CredentialStateStore(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

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