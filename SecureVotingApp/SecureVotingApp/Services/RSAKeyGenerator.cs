//directive for file reading and writing
using System.IO;
//interface and classes for cryptographic algorithms in BouncyCastle RSA
using Org.BouncyCastle.Crypto;
// 
using Org.BouncyCastle.Crypto.Generators;
//utility class for converting between byte arrays and BigInteger PEM file parsing
using Org.BouncyCastle.OpenSsl;
//
using Org.BouncyCastle.Security;

//namespace for grouping services components together
namespace SecureVotingApp.Services;
//generates the RSA key pair for signing and verifying the blind credentials, save to PEM file and persist the private key securely on the server
public class RSAKeyGenerator
{
    //
    public static void GenerateAndSaveKeyPair(string privateKeyPath, int keySize = 3072)
    {
        
    }
}
{
    // generates a new RSA key pair and saves the private key to a PEM file
    public static void GenerateAndSaveKeyPair(string privateKeyPath, int keySize = 2048)
    {
        // create a new RSA key pair generator
        var keyGen = new RsaKeyPairGenerator();
        // initialize the generator with the specified key size
        keyGen.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        // generate the key pair
        AsymmetricCipherKeyPair keyPair = keyGen.GenerateKeyPair();

        // save the private key to a PEM file
        using (var writer = new StreamWriter(privateKeyPath))
        {
            var pemWriter = new PemWriter(writer);
            pemWriter.WriteObject(keyPair.Private);
            pemWriter.Writer.Flush();
        }
    }
}   