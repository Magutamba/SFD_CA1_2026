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

        //conver the raw byte arrays into positive BigInteger values for RSA arithmetic
        cachedModulus = new BigInteger(1, modulusBytes);
        cachedExponent = new BigInteger(1, exponentBytes);
    }

    //generates a blinded voting credential asynchronously for the specified election
    public async Task<BlindCredentialRequest> CreateBlindedCredentialAsync(string electionId)
    {
        //ensure the RSA public key parameters (modulus and exponent) are loaded and cached in memory
        await PublicKeyLoadedAsync();

        //starts as null but will be set after PublicKeyLoadedAsync is called
        BigInteger modulus = cachedModulus!;
        BigInteger exponent = cachedExponent!;

        //calculates the RSA modulus byte length k rounding bits to full octet 
        int k = (modulus.BitLength + 7) / 8;
        //EMSA-PSS padding structure using SHA384 per RFC 9474  
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(electionId);
        // add SHA384 to variable sha384 to use compute HASH encoded message bytes
        using var sha384 = SHA384.Create();
        //hash the electionId message using SHA384 to create a fixed length digest
        byte[] hashedMessage = sha384.ComputeHash(messageBytes);
        //converts the pss encoded message bytes into a positive BigInteger m for RSA arithmetic 
        var m = new BigInteger(1, hashedMessage);
        var pubKey = new RsaKeyParameters(false, modulus, exponent);
        // create a PSS signer with RSA engine and SHA384 digest for signature verification
        var pssSigner = new PssSigner(new RsaEngine(), new Sha384Digest(), 48);
        // initialize the PSS signer with the public key
        pssSigner.Init(true, pubKey);
        // update the PSS signer with the message bytes
        pssSigner.BlockUpdate(messageBytes, 0, messageBytes.Length);
        // generate the PSS encoded signature bytes for the electionId message
        byte[] pssEncodedBytes = pssSigner.GenerateSignature();

        //declare a variable to hold the blinding factor r, while ensuring it is coprime to the RSA modulus N
        BigInteger r;
        do
        {
            // generate a random blinding factor r in the range [1, N-1] where N is the RSA modulus
            byte[] rBytes = RandomNumberGenerator.GetBytes(k);
            //convert the random bytes into a positive BigInteger r for RSA arithmetic
            r = new BigInteger(1, rBytes);

        } while (r.CompareTo(BigInteger.One) <= 0 || r.CompareTo(modulus) >= 0 || !r.Gcd(modulus).Equals(BigInteger.One));
        //blinding the credential using RSA blind signature scheme
        BigInteger rToE = r.ModPow(exponent, modulus);
        //compute the blinded credential as m' = m * r^e mod N
        BigInteger blindedNum = m.Multiply(rToE).Mod(modulus);

        // use the blinded number created above.
        byte[] blindedBytes = blindedNum.ToByteArrayUnsigned();
        //fixed byte array of length k to match the RSA modulus length
        byte[] fixedLenBlinded = new byte[k];

        //left pad the blinded credential with zeros if it is shorter than the modulus length
        Buffer.BlockCopy(blindedBytes, 0, fixedLenBlinded, k - blindedBytes.Length, blindedBytes.Length);

        return new BlindCredentialRequest
        {
            BlindedCredential = Base64UrlEncode(fixedLenBlinded),
            State = new BlindState
            {
                BlindingValue = Base64UrlEncode(r.ToByteArrayUnsigned())
            }

        };

    }

    //unblind the signed blinded credential using the blinding value
    public async Task<string> UnblindCredentialAsync(string signedBlindedCredential, BlindState state)
    {
        await PublicKeyLoadedAsync();

        BigInteger modulus = cachedModulus!;

        //convert the base64 url encoded signed blinded credential to a byte array
        byte[] signedBytes = Base64UrlDecode(signedBlindedCredential);

        //converts the base64 url encoded blinding value to a byte array
        byte[] rBytes = Base64UrlDecode(state.BlindingValue);

        // convert the signed blinded credential bytes into a positive BigInteger s' for RSA arithmetic
        var sPrime = new BigInteger(1, signedBytes);

        //ublinding math
        // convert the blinding value bytes into a positive BigInteger r for RSA arithmetic
        var r = new BigInteger(1, rBytes);

        // compute the modular inverse of the blinding factor r with respect to the RSA modulus N
        BigInteger rInverse = r.ModInverse(modulus);
        // compute the unblinded credential as m = s' * r^(-1) mod N
        BigInteger unBlindedNum = sPrime.Multiply(rInverse).Mod(modulus);
        // calculate the RSA modulus byte length k rounding bits to full octet
        int k = (modulus.BitLength + 7) / 8;
        //convert the unblinded integer back to a fixed length byte array
        byte[] unBlindedBytes = unBlindedNum.ToByteArrayUnsigned();
        // create a fixed length byte array of length k to match the RSA key modulus byte length
        byte[] fixedLenUnBlinded = new byte[k];
        //left pad the unblinded value with zeros if it is shorter than the modulus length
        Buffer.BlockCopy(unBlindedBytes, 0, fixedLenUnBlinded, k - unBlindedBytes.Length, unBlindedBytes.Length);

        // return the unblinded credential as a base64 url encoded string to the client
        return Base64UrlEncode(fixedLenUnBlinded);
    }

    //helper method to decode base64 url encoded string into a byte array
    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    //helper method to decode base64 url encoded string into a byte array
    private static byte[] Base64UrlDecode(string data)
    {
        string padded = data.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}