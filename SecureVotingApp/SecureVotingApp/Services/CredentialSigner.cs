//@author Moïse
// classes for converting strings to and from byte arrays 
// directive for core fundamental types and base classes
using System;
//directives for character encoding and decoding
using System.Text;
//directive for file reading and writing
using System.IO;


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
using System.Security.Cryptography;
using Org.BouncyCastle.Security;

//namespace for grouping services components together
namespace SecureVotingApp.Services;

//performs the server side signing of a blinded credential using RSA blind signature
public class CredentialSigner
{
    //private key used to sign the blinded credential
    private readonly RsaPrivateCrtKeyParameters privateKey;

    // contructor to load the RSA private key from a PEM file
    public CredentialSigner(string privateKeyPath)
    {
        //
        //load the private key once when in the service
        using var reader = File.OpenText(privateKeyPath);
        var pemReader = new PemReader(reader);

        //parse the keyobject from the PEM file
        object keyObject = pemReader.ReadObject()
            ?? throw new InvalidOperationException(
                "The private key could not be loaded."
            );
        //convert the keyObject to Rsa private key parametre
        privateKey = keyObject switch
        {
            // if the keyObject contains a full key pair extract private key portion
            AsymmetricCipherKeyPair keyPair
                => (RsaPrivateCrtKeyParameters)keyPair.Private,
            //if the key object is already a private key parameter use it directly
            RsaPrivateCrtKeyParameters key
                => key,
            _ => throw new InvalidOperationException(
                "The file does not contain a valid RSA private key."
            )
        };
        //ensure the RSA key is at least 2048 bits long for security
        if(privateKey.Modulus.BitLength < 2048)
        {
            throw new CryptographicException("RSA signing keys must be at least 2048 bits long.");
        }
    }

    // returns the public key for verification of the signed blinded credential
    public RsaKeyParameters GetPublicKey()
    {
        //return the public key parameters from the private key
        return new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
    }

    //sign blinded credential using the RSA private key and return it to the client
    public string SignBlindedCredential(string blindedCredential)
    {
        //decode the base64 url encoded blinded credential into a byte array
        byte[] blindedBytes = Base64UrlDecoder(blindedCredential);
        //compute the modulus length in bytes from RSA private key bit length rounding to nearest byte
        int modulusLen = (privateKey.Modulus.BitLength + 7) / 8;
        //checks blinded credential length equals RSA modulus length to prevent malleability attacks
        if (blindedBytes.Length != modulusLen)
        {
            throw new CryptographicException("The blinded credential is out of RSA modulus length");
        }

        //decode the bytes as a positive RSA integer
        var blindNum = new BigInteger(1, blindedBytes);

        //retrieves the RSA modulus
        BigInteger modulus = privateKey.Modulus;

        //reject values outside the RSA range
        if (blindNum.CompareTo(BigInteger.Zero) <= 0 || blindNum.CompareTo(privateKey.Modulus) >= 0)
        {
            throw new CryptographicException(
                "The blinded credential is invalid or out range for RSA modulus."
            );
        }
        //use RSA blinding for private key operations to prevent timing attacks and information leak through repeated signing operations
        var rsaEngine = new RsaBlindedEngine();
        //cryptographycally secure random source for blinding factor generation
        var random = new SecureRandom();
        //initialize the engine for the private RSA operations
        rsaEngine.Init(true, new ParametersWithRandom(privateKey, random));

        //perform the raw RSA private operation on the blinded credential to produce a signed blinded credential
        byte[] signedBytes = rsaEngine.ProcessBlock(blindedBytes,0, blindedBytes.Length);

        //convert the signed blinded credential bytes into a positive BigInteger for RSA arithmetic
        var signedBlindNum = new BigInteger(1, signedBytes);

        //verify the RSA private operation using the public key
        BigInteger verificationNum = signedBlindNum.ModPow(privateKey.PublicExponent,modulus);
        //reject the result if the RSA operation failed the fault check
        if (!verificationNum.Equals(blindNum))
        {
            throw new CryptographicException("RSA signing operation failed verification.");
        }

        //checks that the result matches the RSA modulus length
        if(signedBlindNum.CompareTo(BigInteger.Zero) <=0 || signedBlindNum.CompareTo(modulus)>=0)
        {
            throw new CryptographicException("Invalid RSA signature length.");
        }
        //fixed length byte array same length as RSA modulus
        byte[] fixedLenSigned = new byte[modulusLen];
        //convert the signature to unsigned byte array for transmission to the client
        byte[] unsignedBytes = signedBlindNum.ToByteArrayUnsigned();
        //left pad the signature to the required length
        Buffer.BlockCopy(unsignedBytes,0, fixedLenSigned, modulusLen - unsignedBytes.Length, unsignedBytes.Length);

        //return a signed blind credential to the client
        return Base64UrlEncoder(fixedLenSigned);

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

    //encoded a byte array into a base64 url safe string for safe transmission over HTTP 
    private static string Base64UrlEncoder(byte[] data)
    {
        //replace url safe characters into standar base64 characters
        return Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    }
}

