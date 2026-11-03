//@author Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Client.Contracts;

// contract for requesting a vote session with unblinded credential in browser memory to cast a vote anonymously

public class VoteSession
{
    [Required] 
    //identify the election the voter is voting in
     public string ElectionId {get; set;} = string.Empty;

    //anonymous signed blindend credential issued by server after checking voter is logged in and hasn't voted
    [Required]
    public string AnonymousCredential {get;set;} =string.Empty;
    //vote cast by the voter
    [Required]
    public string VoteChoice {get;set;} 

    //the signed credential expires at this time
    public DateTimeOffset CredentialExpiresAt {get;set;}
}
