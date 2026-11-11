//@author Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System;
// directives to use for using collections and lists
using System.Collections.Generic;
//directives to support asynchronous programming and tasks
using System.Threading.Tasks;
//directives to use the contracts
using SecureVotingApp.Client.Contracts;
// directive to use the data models for the application
using SecureVotingApp.Models;
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

    //validates the credential request and issues a signed blinded credential if the user is eligible
    public async Task<CredentialResponse> IssueAsync(
        int userId, string electionId, CredentialRequest request)
    {
        //validate request
        if (request == null)
        {
            throw new InvalidOperationException("Invalid Credential request.");
        }

        //if userId is invalid or electionId
        if (userId <= 0)
        {
            throw new InvalidOperationException("Invalid userId");
        }

        if (string.IsNullOrWhiteSpace(electionId))
        {

            throw new InvalidOperationException("Invalid electionId");
        }
        //ensure electionId match       
        if (request.ElectionId != electionId)
        {
            throw new InvalidOperationException("The election id does not match");
        }
        //recover requests do not sign new credentials
        if (string.IsNullOrWhiteSpace(request.BlindedCredential))
        {
            var recovered = await RecoverAsync(userId, electionId);
            if (recovered == null)
            {
                throw new InvalidOperationException("No issued credential found for recovery");
            }
            return recovered;
        }
        //validate fields for new issuancee
        if (string.IsNullOrWhiteSpace(request.RecoveryPackage))
        {
            throw new InvalidOperationException("Recovery package is missing");
        }
        //check if the user already has a credential issuance
        var existing = await stateStore.GetExistingIssuedCredentialAsync(userId, electionId);
        //return the existing credential if it has already been issued
        if (existing != null)
        {
            if (existing.BlindedCredential != request.BlindedCredential
                 || existing.RecoveryPackage != request.RecoveryPackage )
            {
                throw new InvalidOperationException("A credential has already been issued, recover the original credential.");
            }
            //return orignial blinded credential
            if (existing.Status == CredentialStatus.Issued)
                return CreateResponse(existing);

            return await CompleteIssuanceAsync(existing);
        }

        //reserve the credential for ths user before signing to prevent conflicts with concurrent requests
        bool reserved = await stateStore.ReserveCredentialIssuedAsync(userId, electionId,
                request.BlindedCredential, request.RecoveryPackage);
        if (!reserved)
        {
            existing = await stateStore.GetExistingIssuedCredentialAsync(userId, electionId);
            if (existing == null || existing.BlindedCredential != request.BlindedCredential ||
               existing.RecoveryPackage != request.RecoveryPackage)
            {
                throw new InvalidOperationException("A credential has already been issued");

            }


            if (existing.Status == CredentialStatus.Issued)
                return CreateResponse(existing);

            //complete the issuance for the thread that reserved the credential and return the signed credential
            return await CompleteIssuanceAsync(existing);
        }


        var pending = await stateStore.GetExistingIssuedCredentialAsync(userId, electionId);
        if (pending == null)
        {
            throw new InvalidOperationException("Credential reservation could not be retrieved");
        }
        return await CompleteIssuanceAsync(pending);

    }
    //sign and persit the original blinded credential 

    private async Task<CredentialResponse> CompleteIssuanceAsync(IssuedCredential record)
    {
        try
        {
            //sign the blinded credential using the server private signing key
            string signedValue = credentialSigner.SignBlindedCredential(record.BlindedCredential);

            //saved signed response and mark issuance as completed
            bool completed = await stateStore.CompleteIssuedCredentialAsync(record.UserId, record.ElectionId, signedValue);
            if (!completed)
            {
                throw new InvalidOperationException("Credential issuancecould not be completed");
            }

            //update the record status locally to reflect the completed issuance
            record.SignedBlindedCredential = signedValue;
            record.Status = CredentialStatus.Issued;

            return CreateResponse(record);
        }
        catch
        {
            //release the reserved credential only if signing fails
            await stateStore.ReleaseIssuedCredentialAsync(record.UserId, record.ElectionId);
            throw;
        }
    }

    //recover original credential. issuance
    public async Task<CredentialResponse?> RecoverAsync(
        int userId, string electionId)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(electionId))
        {
            return null;
        }

        //retrieve the original issued credential for the user and election
        var existing = await stateStore.GetRecoveryCredentialAsync(userId, electionId);
        if (existing == null)
        {
            return null;
        }

        return CreateResponse(existing);
    }
    //method for creating response
    private static CredentialResponse CreateResponse(IssuedCredential credential)
    {
        return new CredentialResponse
        {
            ElectionId = credential.ElectionId,
            SignedBlindedCredential = credential.SignedBlindedCredential,
            RecoveryPackage = credential.RecoveryPackage,
            Choices = new List<string> { "Yes", "No" }
        };
    }
}