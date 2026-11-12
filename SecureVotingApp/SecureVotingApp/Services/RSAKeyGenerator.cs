//@author Moïse
//directive for file reading and writing
using System.IO;
//interface and classes for cryptographic algorithms in BouncyCastle RSA
using Org.BouncyCastle.Crypto;
// classes for generating RSA key pairs
using Org.BouncyCastle.Crypto.Generators;
//utility class for converting between byte arrays and BigInteger PEM file parsing
using Org.BouncyCastle.OpenSsl;
// generating secure random number for cryptogrpahic operations
using Org.BouncyCastle.Security;

//namespace for grouping services components together
namespace SecureVotingApp.Services;

public class RSAKeyGenerator
{
    //generates the RSA key pair for signing and verifying the blind credentials, save to PEM file and persist the private key securely on the server
    public static void GenerateAndSaveKeyPair(string privateKeyPath, int keySize = 3072)
    {
        if (File.Exists(privateKeyPath))
        {
            Console.WriteLine("RSA key pair already exists.");
            return;
        }
        //create a new RSA key pair generator
        var keyGen = new RsaKeyPairGenerator();
        //initialize the generator with the specified key size
        keyGen.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        //generate the key pair
        AsymmetricCipherKeyPair keyPair = keyGen.GenerateKeyPair();

        //create directory if it doesn't exist
        string? directory = Path.GetDirectoryName(privateKeyPath);
        //check if directory is not null or empty before creating it
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        //save the private key to a PEM file
        using (var writer = new StreamWriter(privateKeyPath))
        {
            // write the private key in PEM format
            var pemWriter = new PemWriter(writer);
            pemWriter.WriteObject(keyPair.Private);
            pemWriter.Writer.Flush();

            Console.WriteLine("RSA key pair generated successfully");
        }

    }
}   