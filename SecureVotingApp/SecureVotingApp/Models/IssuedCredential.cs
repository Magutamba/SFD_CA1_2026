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
    //orginal blind credntial created in client
    public string BlindedCredential { get; set; } = string.Empty;
    //server signed blindend credential 
    public string SignedBlindedCredential {get;set;} =string.Empty;
    
}