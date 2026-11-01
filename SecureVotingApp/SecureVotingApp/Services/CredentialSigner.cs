//@author Moïse
// classes for converting strings to and from byte arrays 
using System.Text;

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

//namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//performs the server side signing of a blinded credential using RSA blind signature
public class CredentialSigner
{
    //private key used to sign the blinded credential
    private readonly RsaPrivateCrtKeyParameters privateKey;

    // contructor to load the RSA private key from a PEM file
    public CredentialSigner(string privateKeyPath)
    {
        //load the private key once when in the service
        using var reader = File.openText(privateKeyPath);
        var pemReader = new PemReader(reader);

        //parse the keyobject from the PEM file
        object keyObject = pemReader.ReadObject()
            ?? throw new InvaliOperationException(
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
    }

    //sign blinded credential using the RSA private key and return it to the client
    public string SignBlindedCredential(string blindedCredential)
    {
        //decode the bytes as a positive RSA integer
        var blindNum = new BigInteger(1, blindedBytes);

        //reject values outside the RSA range
        if (blindNum.CompareTo(BigInteger.One) < 0 || blindNum.CompareTo(privateKey.Modulus) >= 0)
        {
            throw new InvalidOperationException(
                "The blinded credential is invalid or out range for RSA modulus."
            );
        }

        //perform RSA signed blinding operation
        var signedBlindNum = blindNum.ModPow(privateKey.Exponent, privateKey.Modulus);

        //convert the signed value to a fixed RSA byte array
        //1 ensures value is positive regardless of high bit value
        int modulusLen = (privateKey.Modulus.BitLength + 7) / 8;

        //convert the signed BigInteger to a unsigned byte array to match RSA modulus length
        byte[] signedBytes = signedBlindNum.ToByteArrayUnsigned();

        //fixed length byte array same length as RSA modulus
        byte[] fixedLenSigned = new byte[modulusLen];

        //left pad the signed value with zeros if it is shorter than the modulus length
        buffer.BlockCopy(signedBytes, 0, fixedLenSigned, modulusLen - signedBytes.Length, signedBytes.Length);

        //return a signed blind credential to the client
        return Base64UrlEncoder.Encode(fixedLenSigned);

    }

    //deocodes a  raw base64 string back into a raw byte array for cryptographic operations 
    private static byte[] Base64UrlDecoder(string data)
    {
        //replace url safe characters into standar base64 characters
        string padded = data.Replace('-', '+').Replace('_', '/');

        //pad the string with '-' character so its a mutliple of 4
        padded += new string('-', 4 - padded.Length % 4);

        return Convert.FromBase64String(padded);
    }

    //encoded a byte array into a base64 url safe string for safe transmission over HTTP 
    private static string Base64UrlEncoder(string data)
    {
        //replace url safe characters into standar base64 characters
        return Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('-');

    }
}