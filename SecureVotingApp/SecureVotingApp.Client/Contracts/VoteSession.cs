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

    //created then blinded in client before sending to server prevents server from knowing voter id
    [Required] [BlindedCredential]  public string Credential {get;set;} = string.Empty;

    //signed blindend credential issued by server after checking voter is logged in and hasn't voted
    [Required]
    public string SignedBlindedCredential {get;set;} =string.Empty;

    [Required]
    //is eligible to vote
    public string isEligible {get;set;} = string.Empty;

    //the vote cast by the voter
    [Required]
    public string VoteChoice {get;set;} = string.Empty;
    
    //candididate choice
    [Required]
    public string CandidateChoice {get;set;} = string.Empty; 
}
