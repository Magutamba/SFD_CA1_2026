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
using System.Linq;
using System.Security.Cryptography;

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
        //check all issued credentials
        return await dbContext.IssuedCredentials.AnyAsync(ic =>
            ic.UserId == userId && ic.ElectionId == electionId
        );
    }

    // reserve one issued credential per voter per election to prevent multiple credential issuance
    // async to not block the server thread waiting for the db to connect
    public async Task<bool> ReserveCredentialIssuedAsync(int userId, string electionId, string blindedCredential, string recoveryPackage)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId) || string.IsNullOrWhiteSpace(blindedCredential)
                 || string.IsNullOrWhiteSpace(recoveryPackage))
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
            var credentialReserved = await dbContext.IssuedCredentials.FirstOrDefaultAsync(
                ic => ic.UserId == userId && ic.ElectionId == electionId
            );
            // do not create a new credential reservation if one already exists
            if (credentialReserved != null)
            {
                await transaction.RollbackAsync();
                return false;
            }


            //check the database for existing issued credential for the user and election
            issuedCredential = new IssuedCredential
            {
                UserId = userId,
                ElectionId = electionId,
                BlindedCredential = blindedCredential,
                Status = CredentialStatus.Pending,
                RecoveryPackage = recoveryPackage

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
    //retrieve an existing issued credential or completed issuance
    public async Task<IssuedCredential?> GetExistingIssuedCredentialAsync(int userId, string electionId)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId))
        {
            return null;
        }
        //only retrieve the issued credential belonging to the authenticated user
        return await dbContext.IssuedCredentials.AsNoTracking().SingleOrDefaultAsync(ic =>
            ic.UserId == userId && ic.ElectionId == electionId
        );
    }

    //complete a pending reservation after successful signing
    public async Task<bool> CompleteIssuedCredentialAsync(int userId, string electionId, string signedBlindedCredential)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId) || string.IsNullOrWhiteSpace(signedBlindedCredential))
        {
            return false;
        }
        //retrieve the pending issued credential for the user and election
        var issuedCredential = await dbContext.IssuedCredentials.SingleOrDefaultAsync(ic =>
            ic.UserId == userId && ic.ElectionId == electionId && ic.Status == CredentialStatus.Pending
        );
        if (issuedCredential == null)
        {
            return false;
        }
        //update the issued credential with the signed blinded credential and mark as issued
        issuedCredential.SignedBlindedCredential = signedBlindedCredential;
        //change the server controlled status to issued
        issuedCredential.Status = CredentialStatus.Issued;
        //persist the completed issuance
        await dbContext.SaveChangesAsync();
        return true;
    }
    //retrieve original issued credential for recovery using recoveryId and electionId
    public async Task<IssuedCredential?> GetRecoveryCredentialAsync(int userId, string electionId)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId))
        {
            return null;
        }
        //retrieve the issued credential for the user and election 
        var record = await GetExistingIssuedCredentialAsync(userId, electionId);
        if(record== null || record.Status != CredentialStatus.Issued )
        {
            return null;
        }

          return record;
    }
    //release an issued credential if signing fails before successful issuance
    public async Task ReleaseIssuedCredentialAsync(int userId, string electionId)
    {
        //reject invalid userId or electionId
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId))
        {
            return;
        }
        //atomically remove the pending record,  
        await dbContext.IssuedCredentials.Where(ic =>
                ic.UserId == userId && ic.ElectionId == electionId &&
                  ic.Status == CredentialStatus.Pending).ExecuteDeleteAsync();
    }

        //decodes a  raw base64 string back into a raw byte array for cryptographic operations 
    private static byte[] Base64UrlDecoder(string data)
    {
        //replace url safe characters into standar base64 characters
        string padded = data.Replace('-', '+').Replace('_', '/');

        //pad the string with '-' character so its a mutliple of 4
        padded += new string('=', (4 - padded.Length % 4) % 4);

        return Convert.FromBase64String(padded);
    }
}