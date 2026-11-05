//@author Moïse
// directive for core fundamental types and base classes
using System;
//directives to use cryptographic primitives such as SHA384 and RSA
using System.Security.Cryptography;
//directives to support asynchronous programming and tasks
using System.Threading.Tasks;
//interface and classes for cryptographic algorithms in BouncyCastle RSA
using Org.BouncyCastle.Crypto;
//cryptographic key parameter for RSA orivate key
using Org.BouncyCastle.Crypto.Parameters;
//BigInteger class for RSA arithmetic
using Org.BouncyCastle.Math;
//utility class for converting between byte arrays and BigInteger PEM file parsing
using Org.BouncyCastle.OpenSsl;
//provides MVC and web API functionality API controller, HTTP request handlers
using Microsoft.AspNetCore.Mvc;

// directive to use Entity Framework Core for database operations LINQ, AnyAsync and SaveChangesAsync
using Microsoft.EntityFrameworkCore;
//directive to use AppDbContext for database queries and updates
using SecureVotingApp.Data;
// directive to use the data models for the application
using SecureVotingApp.Models;

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
[Route("api/votes")]

//class for handling vote submission requests from the client
public class VoteController : ControllerBase
{
    //database context for accessing the database
    private readonly AppDbContext dbContext;
    //credential signer for signing blinded credentials
    private readonly CredentialSigner credentialSigner;

    //***symplify application, but allows for future extension
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
            Modulus =Base64UrlEncoder(pubKey.Modulus.ToByteArrayUnsigned()),
            // * FIXED: RsaKeyParameters exposes the exponent as Exponent.
            Exponent = Base64UrlEncoder(pubKey.Exponent.ToByteArrayUnsigned())
        });
    }

    //handle post request to submit ballot
    [HttpPost]
    //FromBody attribute indicates the request body will be deserialized into the AnonymousVoteSubmit object
    public async Task<IActionResult> SubmitVote([FromBody]AnonymousVoteSubmit request)
    {  
        //validate the request model
        if(string.IsNullOrWhiteSpace(request.AnonymousCredential) ||string.IsNullOrWhiteSpace(request.ElectionId))
        {
            return BadRequest("Invalid request data.");
        }

        //variable for signed credential bytes
        byte[] signatureBytes;
        try
        {
            //decode the base64 url signature of the enconded credential
            signatureBytes = Base64UrlDecoder(request.AnonymousCredential);

        }
        catch
        {
            return BadRequest("Invalid credential encoding.");
        }

        //verify the signature of the blinded credential using the server public keu
        if(!VerifyCredentialSignature(request.ElectionId, signatureBytes))
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

        //check if the hash of the blinded credential has already been spent
        bool beenSpent = await dbContext.SpentCredentials.AnyAsync(sbc =>sbc.ElectionId ==request.ElectionId && sbc.CredentialHash == credentialHash);
    
        if (beenSpent)
        {
            return Conflict(new {message = "This voting credential has already been used."});
        }
    
        //record the hash of the blinded credential as spent to prevent double voting
        dbContext.SpentCredentials.Add(new SpentCredential
        {
            ElectionId = request.ElectionId,
            CredentialHash = credentialHash,
            SpentAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Ok(new VoteReceipt
        {
             ElectionId = request.ElectionId,
             //generate a unique receipt id for the vote submission
             ReceiptId = Guid.NewGuid().ToString(),
             ConfirmationMessage = "Your vote has successfully been submitted."

        });
    }
        
        private bool VerifyCredentialSignature(string electionId, byte[]signatureBytes)
        {
            try
            {   
                //retrieve the server RSA public key parameters(Modulus N and exponent e) from the credential signer
                RsaKeyParameters publicKey = credentialSigner.GetPublicKey();
                // converts the raw signature bytes into a positive BigInteger scalar s for modular arithmetic
                var s = new BigInteger(1, signatureBytes);

                //reject signature integers out of RSA
                if(s.CompareTo(BigInteger.One)<= 0 || s.CompareTo(publicKey.Modulus) >= 0)
                {
                    return false;
                }
                //RFC 9474 verification of RSA signature using public key and exponent
                BigInteger m = s.ModPow(publicKey.Exponent, publicKey.Modulus);
                //convert recoverd integer back to fixed octet array matching the RSA modulus length k
                int k = (publicKey.Modulus.BitLength + 7 ) / 8;
                //convert the recovered RSA integer m to an unsigned byte array 
                byte [] recoveredBytes = m.ToByteArrayUnsigned();
                //create a fixed length byte array of length k to match the RSA key modulus byte length 
                byte [] fixedLenRecovered = new byte[k];
                //left pad the recovered value with zeros if it is shorter than the modulus length
                Buffer.BlockCopy(recoveredBytes, 0, fixedLenRecovered, k - recoveredBytes.Length, recoveredBytes.Length);
                
                //compare the fixed length recovered bytes with the original signature bytes to verify the signature
                return fixedLenRecovered.Length == k;
            }
            catch
            {
              return false;
            }
        }
        //decode the base64 url encoded blinded credential into a byte array
        private static byte[] Base64UrlDecoder(string data)
        {
            string padded = data.Replace('-','+').Replace('_','/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            return Convert.FromBase64String(padded);
        }
        //encode bytes to base64 url for public key endpoint
        private static string Base64UrlEncoder(byte[] data)
        {
            return Convert.ToBase64String(data).Replace('+', '-').Replace('/','_').TrimEnd('=');
        }
}