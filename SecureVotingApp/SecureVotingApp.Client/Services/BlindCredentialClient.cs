//@author Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System;
//directives to use cryptographic primitives such as SHA384 and RSA
using System.Security.Cryptography;
//directive for sending HTTP requests and receiving HTTP response
using System.Net.Http;
//directive for sending and receiving JSON content in HTTP requests and response
using System.Net.Http.Json;
//directives to support asynchronous programming and tasks
using System.Threading.Tasks;
//cryptographic key parameter for RSA private key
using Org.BouncyCastle.Crypto.Parameters;
// bouncy caste signers and digests for RFC 9474 compliant EMSA-PSS verification
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Digests;
// directive for BouncyCastle BigInteger class RSA arithmetic
using Org.BouncyCastle.Math;
//engine for RSA encryption and decryption
using Org.BouncyCastle.Crypto.Engines;
using SecureVotingApp.Client.Contracts;
//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

// blind random credential payload to server
public class BlindCredentialClient : IBlindCredentialClient
{
    // instance of HttpClient to send requests to the server
    private readonly HttpClient httpClient;
    // cached RSA modulus N stored in browser to avoid redundant key fetches
    private BigInteger? cachedModulus;
    // cached RSA exponent estored in browser to avoid redundant key fetches
    private BigInteger? cachedExponent;
    //RFC 9474 SHA384 PSS salt length 48 bytes 
    private const int PSS_SaltLength = 48;
    //RFC 9474 randomized message prefix is 32 bytes
    private const int RandomizerLength = 32;
    //minimum RSA modulus length
    private const int MinRsaBitLength = 2048;


    //constructor to inject the HttpClient for making requests to the server
    public BlindCredentialClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    //helper DTO matching the GET /api/votes/keys endpoint response
    public record PublicKeyDTO(string Modulus, string Exponent);


    //fetch the RSA public key dynamically from server and cache it for future use
    public async Task PublicKeyLoadedAsync()
    {
        if (cachedModulus != null && cachedExponent != null) return;
        //fetches and deserializes the public key from the server endpoint into a public key DTO object
        PublicKeyDTO pubKeyDTO = await httpClient.GetFromJsonAsync<PublicKeyDTO>("api/vote/keys")
            ?? throw new InvalidOperationException("Failed to load public RSA key from server.");

        //decode the base64url encoded RSA modulus and exponent into raw byte array
        byte[] modulusBytes = Base64UrlDecode(pubKeyDTO.Modulus);
        byte[] exponentBytes = Base64UrlDecode(pubKeyDTO.Exponent);
        

        BigInteger modulus = new BigInteger(1, modulusBytes);
        BigInteger exponent = new BigInteger(1, exponentBytes);
        //validate RSA modulus length
        if(modulus.BitLength <MinRsaBitLength || !modulus.TestBit(0))
        {
            throw new CryptographicException("RSA modulus must be odd and at least 2048 bits.");
        }
        //validate public exponent
        if(exponent.CompareTo(BigInteger.ValueOf(3)) <0 || !exponent.TestBit(0))
        {
            throw new CryptographicException("Invalid RSA public exponent.");
        }



        //conver the raw byte arrays into positive BigInteger values for RSA arithmetic
        cachedModulus = modulus;
        cachedExponent = exponent;
    }

    //generates a blinded voting credential asynchronously for the specified election
    public async Task<BlindCredentialRequest> CreateBlindedCredentialAsync(string electionId)
    {
        if (string.IsNullOrWhiteSpace(electionId))
        {
            throw new InvalidOperationException("ElectionId is required.");
        }
        //ensure the RSA public key parameters (modulus and exponent) are loaded and cached in memory
        await PublicKeyLoadedAsync();

        //starts as null but will be set after PublicKeyLoadedAsync is called
        BigInteger modulus = cachedModulus!;
        BigInteger exponent = cachedExponent!;

        //calculates the RSA modulus byte length k rounding bits to full octet 
        int k = (modulus.BitLength + 7) / 8;
        //included electionId and expiry time in the blind message to prevent replay attacks and ensure credential validity
        string message = electionId;
        //EMSA-PSS padding structure using SHA384 per RFC 9474  
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        //generates a random 32 byte prefix for the prepared message as required by RFC 9474 to prevent signature malleability
        byte[] messageRandomizer = RandomNumberGenerator.GetBytes(RandomizerLength);
        //append the prefix to the electionId message bytes to create the prepared message for signing
        byte[] preparedMessage = new byte[RandomizerLength + messageBytes.Length];
        //copies the randomizer prefix to the start of the prepared message
        Buffer.BlockCopy(messageRandomizer, 0, preparedMessage, 0, RandomizerLength);
        //copies the electionId after the randomizer prefix in the prepared message
        Buffer.BlockCopy(messageBytes, 0, preparedMessage, RandomizerLength, messageBytes.Length);

        //public key parameters for RSA signature verification using BouncyCastle, false indicates it is a public key
        var pubKey = new RsaKeyParameters(false, modulus, exponent);


        //declare a variable to hold the inverse blinding factor r, while ensuring it is coprime to the RSA modulus N
        BigInteger r;
        do 
        {
            // generate a random blinding factor r in the range [1, N-1] where N is the RSA modulus
            byte[] rBytes = RandomNumberGenerator.GetBytes(k);
            //convert the random bytes into a positive BigInteger r for RSA arithmetic
            r = new BigInteger(1, rBytes);

        } while (r.CompareTo(BigInteger.One) <= 0 || r.CompareTo(modulus) >= 0 || !r.Gcd(modulus).Equals(BigInteger.One));

        //RSA blinding parameters using public key and random blinding factor
        var blindingParams = new RsaBlindingParameters(pubKey, r);
        //create a PSS  signer using RSA blinding and sha384 digest, MGF1(mark generation function 1) byte mask obsucre prepared message and salt
        var pssSigner = new PssSigner(new RsaBlindingEngine(), new Sha384Digest(), new Sha384Digest(), PSS_SaltLength);
        //initialize the PSS encoding and RSA blinding
        pssSigner.Init(true, blindingParams);
        //add the prepared message to the PSS signer for blinding and signing
        pssSigner.BlockUpdate(preparedMessage, 0, preparedMessage.Length);
        //generates the PSS encoded and RSA blinded credential
        byte[] blindedBytes = pssSigner.GenerateSignature();
        //ensure the blinded credential as the same RSA modulus length
        if (!blindedBytes.Length.Equals(k))
        {
            throw new CryptographicException("Invalid blinded credential length");
        }

        return new BlindCredentialRequest
        {
            //encoded blinded credential to send to the server for signing
            BlindedCredential = Base64UrlEncode(blindedBytes),
            //store information required to blind the signature
            State = new BlindState
            {
                BlindingValue = Base64UrlEncode(r.ToByteArrayUnsigned()),
                MessageRandomizer = Base64UrlEncode(messageRandomizer),
                ElectionId = electionId,
                CredentialExpiresAt = DateTimeOffset.MaxValue

            }


        };

    }

    //unblind the signed blinded credential using the blinding value
    public async Task<string> UnblindCredentialAsync(string signedBlindedCredential, BlindState state)
    {
        await PublicKeyLoadedAsync();
        //reject missing state
        if(state == null || string.IsNullOrWhiteSpace(state.BlindingValue)
                  || string.IsNullOrWhiteSpace(state.MessageRandomizer)||
                     string.IsNullOrWhiteSpace(state.ElectionId))
        {
            throw new InvalidOperationException("Invalid blind state");
        }
        //retrieve the cached RSA modulus
        BigInteger modulus = cachedModulus!;
        // calculate the RSA modulus byte length k rounding bits to full octet
        int k = (modulus.BitLength + 7) / 8;
        //decode the base64 url encoded signed blinded credential to a byte array
        byte[] signedBytes = Base64UrlDecode(signedBlindedCredential);
        //checks returned signature as expected length
        if (!signedBytes.Length.Equals(k))
        {
            throw new CryptographicException("Invalid signed blinded credential length");
        }
        //decode the base64 url encoded blinding value to a byte array
        byte[] rBytes = Base64UrlDecode(state.BlindingValue);
        //validate the blinding value
        if(rBytes.Length > k)
        {
            throw new CryptographicException("Invalid blinding factor length");
        }

        // convert the blinding value bytes into a positive BigInteger r for RSA arithmetic
        BigInteger r = new BigInteger(1, rBytes);
        //check if that the blinding value is valid and coprime to the RSA modulus N
        if(r.CompareTo(BigInteger.One)<0 || r.CompareTo(modulus)>=0 || !r.Gcd(modulus).Equals(BigInteger.One))
        {
            throw new CryptographicException("Invalid blinding value");
        }

        // convert the signed blinded credential bytes into a positive BigInteger s' for RSA arithmetic
        BigInteger sPrime = new BigInteger(1, signedBytes);
        //reject a signed representative is outside the RSA range
        if(sPrime.CompareTo(BigInteger.Zero)<=0 || sPrime.CompareTo(modulus) >= 0)
        {
            throw new CryptographicException("Invalid signed blinded credential");
        }
        //ublinding math
        // compute the modular inverse of the blinding factor r with respect to the RSA modulus N
        BigInteger rInverse = r.ModInverse(modulus);
        // compute the unblinded credential as m = s' * r^(-1) mod N
        BigInteger unBlindedNum = sPrime.Multiply(rInverse).Mod(modulus);
        //convert the unblinded integer back to a fixed length byte array
        byte[] unBlindedBytes = unBlindedNum.ToByteArrayUnsigned();
        //ensure the result cannot exceed the RSA modulus length
        if(unBlindedBytes.Length > k)
        {
            throw new CryptographicException("Invalid unblinded credential length");
        }
        // create a fixed length byte array of length k to match the RSA key modulus byte length
        byte[] fixedLenUnBlinded = new byte[k];
        //left pad the unblinded value with zeros if it is shorter than the modulus length
        Buffer.BlockCopy(unBlindedBytes, 0, fixedLenUnBlinded, k - unBlindedBytes.Length, unBlindedBytes.Length);
        //included electionId and expiry time in the blind message to prevent replay attacks and ensure credential validity
        string message = state.ElectionId;
         //retrive the original electionId as UTF8 bytes 
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

        //ensure message prefix is 32 bytes exaclty
        byte[] randomizer = Base64UrlDecode(state.MessageRandomizer);
        if(randomizer.Length !=RandomizerLength)
        {   
            throw new CryptographicException("Invalid message randomizer");
        }
        //reconstruct original prepared message
        byte[] preparedMessage = new byte[RandomizerLength + messageBytes.Length];

        //copy randomized prefix into prepared message
        Buffer.BlockCopy(randomizer,0, preparedMessage,0, randomizer.Length );

        //append the electionId to the prepared message
        Buffer.BlockCopy(messageBytes,0, preparedMessage, RandomizerLength, messageBytes.Length);
        //retrieve the public key for signature verification
        var pubKey = new RsaKeyParameters(false, modulus, cachedExponent);
        //create a PSS signer with RSA engine and SHA384 digest to verify the unblinded credential signature
        var verifier = new PssSigner(new RsaEngine(), new Sha384Digest(), new Sha384Digest(),  PSS_SaltLength);

        //intialize signature verification with the public key
        verifier.Init(false,pubKey);

        //supply the original prepared message
        verifier.BlockUpdate(preparedMessage,0, preparedMessage.Length);
        //verifies the unblinded credential before returning it to the client
        if (!verifier.VerifySignature(fixedLenUnBlinded))
        {
            throw new CryptographicException("Unblinded credential verification failed");
        }
        // return the unblinded credential as a base64 url encoded string to the client
        return Base64UrlEncode(fixedLenUnBlinded);
    }

    //helper method to decode base64 url encoded string into a byte array
    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    //helper method to decode base64 url encoded string into a byte array
    private static byte[] Base64UrlDecode(string data)
    {
        //reject empty or null encoded data Base64 
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new FormatException("Invalid Base64 input");
        }
        string padded = data.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
