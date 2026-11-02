//@author Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Components.Contracts;

//

public class VotingSession
{
    [Required] 
    //identify the election the voter is voting in
     public string ElectionId {get; set;} = string.Empty;

    //anonymous signed blindend credential issued by server after checking voter is logged in and hasn't voted
    [Required]
    public string AnonymousCredential {get;set;} =string.Empty;

    [Required]
    //is eligible to vote
    public string isEligible {get;set;} = string.Empty;

    //prevents submission after expiry
    public DateTimeOffset ExpiresAt {get;set;} = string.Empty;
    //token to prevent double voting
    public string AsVotedToke {get;set;} = string.Empty;
    //the vote cast by the voter
    [Required]
    public string VoteChoice {get;set;} 

    //the signed credential expires at this time
    public DateTimeOffSet CredentialExpiresAt {get;set;}
}
