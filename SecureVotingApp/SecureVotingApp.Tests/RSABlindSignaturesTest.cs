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
// json serializationa and deserialization
using System.Text.Json;
//provides core cryptographic classes including InvalidCipherTextException
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;
// classes for generating RSA key pairs
using Org.BouncyCastle.Crypto.Generators;
//utility class for converting between byte arrays and BigInteger PEM file parsing
using Org.BouncyCastle.OpenSsl;
// generating secure random number for cryptogrpahic operations
using Org.BouncyCastle.Security;
//logical namespace for grouping services components together

namespace SecureVotingApp.Tests;

//class for testing RSA blind signature generation and verification

public class RSABlindSignaturesTest
{
    [Fact]
    public void TestBlindSignature()
    {
        int keySize = 3072;
        int PSS_SaltLength = 48;
        //create a new RSA key pair generator
        var keyGen = new RsaKeyPairGenerator();
        //initialize the generator with the specified key size
        keyGen.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
        //generate the key pair
        AsymmetricCipherKeyPair keyPair = keyGen.GenerateKeyPair();
        //create a message to sign
        byte[] message = System.Text.Encoding.UTF8.GetBytes("Election Test");

        // Create a blind signature
        var signer = new PssSigner(new RsaEngine(), new Sha384Digest(), PSS_SaltLength);
        signer.Init(true, keyPair.Private);
        signer.BlockUpdate(message, 0, message.Length);
        byte[] blindSignature = signer.GenerateSignature();

        // Verify that blind signature is valid
        var verifier = new PssSigner(new RsaEngine(), new Sha384Digest(), PSS_SaltLength);
        verifier.Init(false, keyPair.Public);
        verifier.BlockUpdate(message, 0, message.Length);
        bool isValid = verifier.VerifySignature(blindSignature);

        Assert.True(isValid, "The blind signature is valid.");
    }
}
