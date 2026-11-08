//@author Moïse
//namespace for grouping services components together
namespace SecureVotingApp.Services;
//directives to support asynchronous programming and tasks
using System.Threading.Tasks;
// directive to use Entity Framework Core for database operations LINQ, AnyAsync and SaveChangesAsync
using Microsoft.EntityFrameworkCore;
//directive to use AppDbContext for database queries and updates
using SecureVotingApp.Data;
// directive to use the data models for the application
using SecureVotingApp.Models;

// state store to keep track of credential state during vote session
public class CredentialStateStore
{
    //store issued credentials to prevent doubl voting
    private readonly AppDbContext dbContext;

    //constructor to inject the database context dependency to
    public CredentialStateStore(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }


    // check if voter as already been issued a credential for the election
    public async Task<bool> HasBeenCredentialedAsync(int userId,string electionId)
    {
        //validate userId and electionId
        if(userId<=0 || string.IsNullOrWhiteSpace(electionId))
        {
            return false;
        }

        return await dbContext.IssuedCredentials.AnyAsync(ic =>ic.UserId ==userId && ic.ElectionId==electionId);
    }

    // record one credential issuance per voter, unique database index prevents duplicate issuance
    // async to not block the server thread waiting for the db to connect
    public async Task<bool> TryMarkIssuedAsync(int userId,string electionId)
    {
        //reject invalid userId or electionId
        if(userId<=0 || string.IsNullOrEmpty(electionId))
        {
            return false;
        }

        //check if the voter already as an issued credential
        var existingCredential = await dbContext.IssuedCredentials.FirstOrDefaultAsync(
            ic => ic.UserId == userId && ic.ElectionId == electionId
        );
        //remove the existing credential if found to allow reissuance
        if(existingCredential != null)
        {
            dbContext.IssuedCredentials.Remove(existingCredential);
            await dbContext.SaveChangesAsync();
        }

        //check the database for existin issued credential for the user and election
        var issuedCredential = new IssuedCredential
        {
            UserId=userId,
            ElectionId=electionId,
            IssuedAt=DateTimeOffset.UtcNow
        };

        dbContext.IssuedCredentials.Add(issuedCredential);

        try
        {
            //the unique index on the database will prevent duplicate issuance of credentials for the same voter
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch(DbUpdateException)
        {
            //remove the added entity from the context to prevent tracking issues
            dbContext.Entry(issuedCredential).State = EntityState.Detached;
            // a duplicate userId as requested credentials for the same election
            return false;
        }
    }
    //release an issued credential if signing fails before successful issuance
    public async Task ReleaseIssuedCredentialAsync(int userId,string electionId, DateTimeOffset expiresAt)
    {
        var issuedCredential = await dbContext.IssuedCredentials.FirstOrDefaultAsync(
                ic => ic.UserId == userId && ic.ElectionId == electionId
        );
        //nothing to release  if no credential exists
        if (issuedCredential != null)
        {
            //remove the issued credential from the database
            dbContext.IssuedCredentials.Remove(issuedCredential);
            try
            {
                //save changes to the database
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                //detach the entry if concurrent operations modified the database to prevent tracking issues
                dbContext.Entry(issuedCredential).State = EntityState.Detached;
                throw;
            }
        }
    }
}