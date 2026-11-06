//@author Moïse
// classes for converting strings to and from byte arrays 
using System.Text;
//
using System;
//
using System.Collections.Generic;
//
//
using System.Security.Cryptography;
using System.Threading.Tasks;
//interface and classes for cryptographic algorithms in BouncyCastle RSA
using Org.BouncyCastle.Crypto;
//low level cipher engine for RSA RsaEngines
using Org.BouncyCastle.Crypto.Engines;
//cryptographic key parameter for RSA orivate key
using Org.BouncyCastle.Crypto.Parameters;
//BigInteger class for RSA arithmetic
using Org.BouncyCastle.Math;
//utility class for converting between byte arrays and BigInteger PEM file parsing
using Org.BouncyCastle.OpenSsl;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using SecureVotingApp.Data;
using SecureVotingApp.Models;
using SecureVotingApp.Client.Contracts;
using SecureVotingApp.Services;
using Microsoft.AspNetCore.Authorization;
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
        
        //Author Aaron Byrne- Check that the voter has infact selected a voting option
        if (string.IsNullOrWhiteSpace(request.VoteChoice))
        {
            return BadRequest("Invalid vote choice, please select an option");
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
        
        //Author Aaron Byrne- When a valid vote has been submitted, create a new anon ballot and save it to the database
        dbContext.AnonBallots.Add(new AnonBallot
        {
            VoteChoice = request.VoteChoice,
            ElectionId = request.ElectionId,
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
                RsaKeyParameters publicKey = credentialSigner.GetPublicKey();
                // s is 
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
                byte [] recoveredBytes = m.ToByteArrayUnsigned();
                byte [] fixedLenRecovered = new byte[k];
                //left pad the recovered value with zeros if it is shorter than the modulus length
                Buffer.BlockCopy(recoveredBytes, 0, fixedLenRecovered, k - recoveredBytes.Length, recoveredBytes.Length);
                
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