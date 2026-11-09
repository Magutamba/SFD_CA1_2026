//@author Moïse
// directive for core fundamental types and base classes
using System;
//directives to use cryptographic primitives such as SHA384 and RSA
using System.Security.Cryptography;
//directives to support asynchronous programming and tasks
using System.Threading.Tasks;
//interface and classes for cryptographic algorithms in BouncyCastle RSA
using Org.BouncyCastle.Crypto;
//cryptographic key parameter for RSA private key
using Org.BouncyCastle.Crypto.Parameters;
//BigInteger class for RSA arithmetic
using Org.BouncyCastle.Math;
//utility class for converting between byte arrays and BigInteger PEM file parsing
using Org.BouncyCastle.OpenSsl;
//provides MVC and web API functionality API controller, HTTP request handlers
using Microsoft.AspNetCore.Mvc;
// bouncy caste signers and digests for RFC 9474 compliant EMSA-PSS verification
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Digests;
// directive to use Entity Framework Core for database operations LINQ, AnyAsync and SaveChangesAsync
using Microsoft.EntityFrameworkCore;
//directive to use AppDbContext for database queries and updates
using SecureVotingApp.Data;
// directive to use the data models for the application
using SecureVotingApp.Models;
//engine for RSA encryption and decryption
using Org.BouncyCastle.Crypto.Engines;

using SecureVotingApp.Client.Contracts;
using SecureVotingApp.Services;
//provide authorization attributes
using Microsoft.AspNetCore.Authorization;
//directive to use rate limiting attributes
using Microsoft.AspNetCore.RateLimiting;

//namespace for grouping services components together
namespace SecureVotingApp.Controllers;

//designates class as Api controller automatical model validation and Http response behaviours
[ApiController]

//allow anonymous access to the controller, so voter can submit ballot anonymously
[AllowAnonymous]
[EnableRateLimiting("StrictPolicy")]
//route for the votecontroller 
[Route("api/vote")]

//class for handling vote submission requests from the client
public class VoteController : ControllerBase
{
    //database context for accessing the database
    private readonly AppDbContext dbContext;
    //credential signer for signing blinded credentials
    private readonly CredentialSigner credentialSigner;

    //symplify application, but allows for future extension
    private const string defaultElectionId = "election-2026";

    //inject the dbcontext and credential signer  into the constructor as dependency injection
    public VoteController(AppDbContext dbContext, CredentialSigner credentialSigner)
    {
        this.dbContext = dbContext;
        this.credentialSigner = credentialSigner;
    }

    //RFC 9474 compliant endpoint to get public key for for blinding and verification of signed blind credentials
    [HttpGet("keys")]
    public IActionResult GetPublicKey()
    {
        //get the public key from the credential signer
        RsaKeyParameters pubKey = credentialSigner.GetPublicKey();
        return Ok(new
        {
            //Modulus and Exponent are encoded in base64url format for safe transmission over HTTP
            Modulus = Base64UrlEncoder(pubKey.Modulus.ToByteArrayUnsigned()),
            // RsaKeyParameters exposes the exponent as Exponent.
            Exponent = Base64UrlEncoder(pubKey.Exponent.ToByteArrayUnsigned())
        });
    }

    //handle post request to submit ballot
    [HttpPost]
    //FromBody attribute indicates the request body will be deserialized into the AnonymousVoteSubmit object
    public async Task<IActionResult> SubmitVote([FromBody] AnonymousVoteSubmit request)
    {   //validate the request
        if (request == null)
        {
            return BadRequest("Request body is required.");
        }
        //validate the request model
        if (string.IsNullOrWhiteSpace(request.AnonymousCredential) || string.IsNullOrWhiteSpace(request.VoteChoice)
               || string.IsNullOrWhiteSpace(request.MessageRandomizer))
        {
            return BadRequest("Invalid request data, missing.");
        }

        //***safe guard to ensure electionId is not null or empty use  the defaultElectionId if it is
        string electionId = string.IsNullOrWhiteSpace(request.ElectionId) ? defaultElectionId : request.ElectionId;
        //variable for signed credential bytes
        byte[] signatureBytes;
        //delcare the randomized message preifx byte array
        byte[] messageRandomizer;
        try
        {
            //decode the base64 url signature of the enconded credential
            signatureBytes = Base64UrlDecoder(request.AnonymousCredential);
            //decode the RFC 9474 message randomizer
            messageRandomizer = Base64UrlDecoder(request.MessageRandomizer);

        }
        catch
        {
            return BadRequest("Invalid credential encoding.");
        }

        //verify the signature of the blinded credential using the server public key
        if (!VerifyCredentialSignature(electionId, signatureBytes, messageRandomizer))
        {
            return Unauthorized("Invalid voting credential signature.");
        }

        //compute SHA384 hash of blind credential to enforce single use
        // add SHA384 to variable sha384to use compute the hash of the blinded credential
        using var sha384 = SHA384.Create();
        // hash the blinded credential
        byte[] hashBytes = sha384.ComputeHash(signatureBytes);

        // convert the hash to a base64 string for storage and comparison
        string credentialHash = Convert.ToBase64String(hashBytes);


        //start a database transaction to ensure atomicity of the vote submission and credential spending
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            //check if the hash of the blinded credential has already been spent
            bool beenSpent = await dbContext.SpentCredentials.AnyAsync(sbc => sbc.ElectionId == electionId
                                && sbc.CredentialHash == credentialHash);
            if (beenSpent)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = "This voting credential has already been used." });
            }
            //record the hash of the blinded credential as spent to prevent double voting
            dbContext.SpentCredentials.Add(new SpentCredential
            {
                ElectionId = electionId,
                CredentialHash = credentialHash,
                SpentAt = DateTimeOffset.UtcNow
            });
            //Author Aaron Byrne- When a valid vote has been submitted, create a new anon ballot and save it to the database
            dbContext.AnonBallots.Add(new AnonBallot
            {
                VoteChoice = request.VoteChoice,
                ElectionId = electionId,
            });
            await dbContext.SaveChangesAsync();

            //commit both records together
            await transaction.CommitAsync();
            return Ok(new VoteReceipt
            {
                ElectionId = electionId,
                //generate a unique receipt id for the vote submission
                ReceiptId = Guid.NewGuid().ToString(),
                ConfirmationMessage = "Your vote has successfully been submitted."

            });


        }
        catch (DbUpdateException)
        {
            //rollback both operations if there is a conflict
            await transaction.RollbackAsync();

            //detach failed entity to prevent it from being tracked and causing issues with future requests
            foreach (var entry in dbContext.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
            {
                entry.State = EntityState.Detached;
            }
            return Conflict(new { message = "Database conflict. Credential is spent." });
        }

    }

    private bool VerifyCredentialSignature(string electionId, byte[] signatureBytes, byte[] messageRandomizer)
    {
        try
        {
            //retrieve the server RSA public key parameters(Modulus N and exponent e) from the credential signer
            RsaKeyParameters publicKey = credentialSigner.GetPublicKey();
            //convert recoverd integer back to fixed octet array matching the RSA modulus length k
            int k = (publicKey.Modulus.BitLength + 7) / 8;
            //check the signature length
            if (signatureBytes.Length != k)
            {
                return false;
            }
            // converts the raw signature bytes into a positive BigInteger scalar s for modular arithmetic
            var s = new BigInteger(1, signatureBytes);

            //reject signature integers out of RSA
            if (s.CompareTo(BigInteger.One) <= 0 || s.CompareTo(publicKey.Modulus) >= 0)
            {
                return false;
            }


            //check the randomized message prefix is 32 bytes
            if (messageRandomizer == null || messageRandomizer.Length != 32)
            {
                return false;
            }
            //construct the blinded message from the client
            string message = electionId;
            //converts the electionId into UTF8 bytes
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            //create the prepared message 
            byte[] preparedMessage = new byte[messageRandomizer.Length + messageBytes.Length];
            //copy the randomized prefix into the prepared message
            Buffer.BlockCopy(messageRandomizer, 0, preparedMessage, 0, messageRandomizer.Length);
            //append the electionId to the randomized prefix 
            Buffer.BlockCopy(messageBytes, 0, preparedMessage, messageRandomizer.Length, messageBytes.Length);

            //create a PSS  signer using RSA blinding and sha384 digest, MGF1(mark generation function 1) byte mask obsucre prepared message and salt
            var pssSigner = new PssSigner(new RsaEngine(), new Sha384Digest(), new Sha384Digest(), 48);
            //initialize the PSS encoding and RSA blinding
            pssSigner.Init(false, publicKey);
            //supply the original prepared message
            pssSigner.BlockUpdate(preparedMessage, 0, preparedMessage.Length);
            //intialize the PSS signer with the public key for verification
            return pssSigner.VerifySignature(signatureBytes);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    //decode the base64 url encoded blinded credential into a byte array
    private static byte[] Base64UrlDecoder(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new FormatException("Input data is null or empty.");
        }
        string padded = data.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
    //encode bytes to base64 url for public key endpoint
    private static string Base64UrlEncoder(byte[] data)
    {
        return Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}