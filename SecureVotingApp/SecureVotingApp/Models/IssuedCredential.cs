//@author Moïse
//namespace for grouping models components together
namespace SecureVotingApp.Models;

//records that a user has already received a credential for the election
public class IssuedCredential
{
    public int Id { get; set; }

    //the authenticated users id
    public int UserId { get; set; }

    //identifies the election
    public string ElectionId { get; set; } = string.Empty;

    //use by server to recognise repeated requests for the same credential, prevents issuing multiple credentials to the same user
    public string BlindedCredential { get; set; } = string.Empty;
    //use by server to recognise repeated requests for the same credential, prevents issuing multiple credentials to the same user
    public string SignedBlindedCredential { get; set; } = string.Empty;
    public CredentialStatus Status {get; set;} = CredentialStatus.Pending;
    // encrypted recovery package blind state
    public string RecoveryPackage {get;set;} = string.Empty;
}