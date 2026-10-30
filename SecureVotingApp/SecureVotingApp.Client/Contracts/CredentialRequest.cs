//Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Components.Contracts;

//contract for requesting ballot and credentials from server in order to vote
public class CredentialRequest
{  [Required] 
    //identify the election the voter is voting in
     public string ElectionId {get; set;} = string.Empty;

    //create then blinded in client before sending to server prevents server from knowing voter id
    [Required] [BlindedCredential]  public string Credential {get;set;} = string.Empty;
    
    [Required] 
    //anonymous signed credential issued after
    // authorises the voter to cast their vote without exposing their identity 
    public string AnonymousCredential {get;set;} = string.Empty;

}