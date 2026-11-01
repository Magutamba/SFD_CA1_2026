//@author Moïse
// classes for converting strings to and from byte arrays 
using System.Text;
//namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//performs the server side signing of a blinded credential using RSA blind signature
public class CredentialSigner
{
    private readonly RsaPrivateCrtKeyParameters privateKey;

}