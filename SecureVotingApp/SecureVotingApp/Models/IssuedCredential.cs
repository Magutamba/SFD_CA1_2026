//@author Moïse
//namespace for grouping models components together
namespace SecureVotingApp.Models;

//records that a user has already received a credential for the election
public class IssuedCredential
{
    public int Id {get;set;}

    //the authenticated users id
    public int UserId {get;set;}

    //identifies the election
    public string ElectionId {get;set;} = string.Empty;

    //records when the credential was issued
    public DateTimeOffset IssuedAt {get;set;}
    //records when the credential was expires
    public DateTimeOffset ExpiresAt {get;set;}
    
}