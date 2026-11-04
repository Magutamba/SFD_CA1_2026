//@author Moïse
//
using System;
//
using System.Security.Cryptography;
//
using System.Net.Http;
//
using System.Net.Http.Json;
//
using System.Threading.Tasks;
//
using Org.BouncyCastle.Math;

//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

// blind random credential payload to server
public class BlindCredentialClient : IBlindCredentialClient
{
    // 
    private readonly HttpClient httpClient;
    //
    private BigInteger? cachedModulus;
    //
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
        if(cachedModulus!=null && cachedExponent != null) return;
        PublicKeyDTO pubKeyDTO = await httpClient.GetFromJsonAsync<PublicKeyDTO>("api/votes/keys") 
            ?? throw new InvalidOperationException("Failed to load public RSA key from server.");

        
        byte[] modulusBytes = Base64UrlDecode(pubKeyDTO.Modulus);
        byte[] exponentBytes = Base64UrlDecode(pubKeyDTO.Exponent);

        cachedModulus = new BigInteger(1,modulusBytes);
        cachedExponent = new BigInteger(1, exponentBytes);
    }

    public async Task<BlindCredentialRequest> CreateBlindedCredentialAsync(string electionId)
    {
        await PublicKeyLoadedAsync();

        //starts as null but will be set after PublicKeyLoadedAsync is called
        BigInteger modulus = cachedModulus!;
        BigInteger exponent = cachedExponent!;

        int k = (modulus.BitLength + 7) /8 ;

        byte[] rawCredentialBytes = RandomNumberGenerator.GetBytes(k-1);

        var m = new BigInteger(1, rawCredentialBytes);

        BigInteger r;
        do
        {
            byte[] rBytes =RandomNumberGenerator.GetBytes(k);
            r = new BigInteger(1,rBytes);
        }while(r.CompareTo(BigInteger.One)<=0 || r.CompareTo(modulus)>=0 || !r.Gcd(modulus).Equals(BigInteger.One));
        //blinding the credential using RSA blind signature scheme
        BigInteger rToE = r.ModPow(exponent,modulus);
        BigInteger blindedNum = m.Multiply(rToE).Mod(modulus);

        // * FIXED: use the blinded number created above.
        byte[] blindedBytes = blindedNum.ToByteArrayUnsigned();
        //
        byte[] fixedLenBlinded = new byte[k];

        Buffer.BlockCopy(blindedBytes,0, fixedLenBlinded, k - blindedBytes.Length, blindedBytes.Length);

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

        //
        byte[] signedBytes  = Base64UrlDecode(signedBlindedCredential);

        //
        byte[] rBytes = Base64UrlDecode(state.BlindingValue);

        //
        var sPrime = new BigInteger(1,signedBytes);
 
        //
        var r = new BigInteger(1,rBytes);

        //unblinding math
        BigInteger rInverse = r.ModInverse(modulus);
        BigInteger unBlindedNum = sPrime.Multiply(rInverse).Mod(modulus);
        //
        int k = (modulus.BitLength + 7) /8;
        //convert the unblinded integer back to a fixed length byte array
        byte[] unBlindedBytes = unBlindedNum.ToByteArrayUnsigned();
        byte[] fixedLenUnBlinded = new byte[k];
        Buffer.BlockCopy(unBlindedBytes, 0, fixedLenUnBlinded, k- unBlindedBytes.Length, unBlindedBytes.Length);

        return Base64UrlEncode(fixedLenUnBlinded); 
    }

    //helper method to decode base64 url encoded string into a byte array
    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).Replace('+', '-').Replace('/','_').TrimEnd('=');
    // // helper method to decode base64 url encoded string into a byte array
    // private static byte[] Base64UrlDecoder(string data)=>Base64UrlDecode(data);
    //helper method to decode base64 url encoded string into a byte array
    private static byte[] Base64UrlDecode(string data)
    {
        string padded = data.Replace('-','+').Replace('_', '/');
        padded += new string('=', (4- padded.Length %4)%4);
        return Convert.FromBase64String(padded);
    }




}