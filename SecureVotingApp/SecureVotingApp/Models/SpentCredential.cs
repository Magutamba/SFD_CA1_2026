//@author Moïse
//namespace for grouping models components together
namespace SecureVotingApp.Models;

//records that an anonymous credential has been used and prevents it being used to vote twice
public class SpentCredential
{
    public int Id {get;set;}

    //identifies the election
    public string ElectionId {get;set;} = string.Empty;

    //hash of the anonymous credential
    public string CredentialHash {get;set;} = string.Empty;

    public DateTimeOffset SpentAt {get;set;}
}