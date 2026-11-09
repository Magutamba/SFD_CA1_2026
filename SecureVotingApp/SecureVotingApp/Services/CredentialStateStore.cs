//@author Moïse
//namespace for grouping services components together
namespace SecureVotingApp.Services;
// using system data to support database operations and data access
using System.Data;
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
    public async Task<bool> HasBeenCredentialedAsync(int userId, string electionId)
    {
        //validate userId and electionId
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId))
        {
            return false;
        }

        return await dbContext.IssuedCredentials.AnyAsync(ic =>
            ic.UserId == userId && ic.ElectionId == electionId && ic.ExpiresAt > DateTimeOffset.UtcNow
        );
    }

    // record one credential issuance per voter, unique database index prevents duplicate issuance
    // async to not block the server thread waiting for the db to connect
    public async Task<bool> TryMarkIssuedAsync(int userId, string electionId, DateTimeOffset expiresAt)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrEmpty(electionId))
        {
            return false;
        }
        //reject expired credentials
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        //start a serializable transaction to prevent concurrent credential issues
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        //make entity available in the catch block
        IssuedCredential? issuedCredential = null;
        try
        {
            //check if the voter already as an issued credential
            var existingCredential = await dbContext.IssuedCredentials.FirstOrDefaultAsync(
                ic => ic.UserId == userId && ic.ElectionId == electionId
            );
            issuedCredential = existingCredential;
            if (existingCredential != null)
            {
                //if the credential is expired the voter can request a new credential
                if (existingCredential.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    await transaction.RollbackAsync();
                    return false;

                }
                //existing credential is expired, update rather than remove
                existingCredential.IssuedAt = DateTimeOffset.UtcNow;

                //store new expiry time
                existingCredential.ExpiresAt = expiresAt;
                // assign the existing credential to the issuedCredential so catch block can detach if it save fails
                issuedCredential = existingCredential;

                //save the updated credential record
                await dbContext.SaveChangesAsync();
                //commit the transaction after successful credential insertion
                await transaction.CommitAsync();
                return true;
            }


            //check the database for existing issued credential for the user and election
            issuedCredential = new IssuedCredential
            {
                UserId = userId,
                ElectionId = electionId,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiresAt
            };

            dbContext.IssuedCredentials.Add(issuedCredential);
            //the unique index on the database will prevent duplicate issuance of credentials for the same voter
            await dbContext.SaveChangesAsync();
            //commit the transaction after successful credential insertion
            await transaction.CommitAsync();
            return true;

        }
        catch (DbUpdateException)
        {
            //detach only if new credential was added
            if (issuedCredential != null)
            {
                //remove the added entity from the context to prevent tracking issues
                dbContext.Entry(issuedCredential).State = EntityState.Detached;
            }
            //rollback the transaction after conflict to prevent partial updates
            await transaction.RollbackAsync();
            // a duplicate userId as requested credentials for the same election
            return false;
        }

    }
    //release an issued credential if signing fails before successful issuance
    public async Task ReleaseIssuedCredentialAsync(int userId, string electionId, DateTimeOffset expiresAt)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrEmpty(electionId))
        {
            return false;
        }
        //atomically remove the exact issued credential 
        await dbContext.IssuedCredentials.Where(ic => 
                ic.UserId == userId && ic.ElectionId == electionId &&
                  ic.ExpiresAt == expiresAt).ExecuteDeleteAsync();
    }
}