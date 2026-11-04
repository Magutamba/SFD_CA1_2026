//@author Moïse 
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Client.Contracts;
//contract for requesting ballot and credentials from server in order to vote
public class CredentialRequest
{  [Required] 
    //identify the election the voter is voting in
     public string ElectionId {get; set;} = string.Empty;

    //created then blinded in client before sending to server prevents server from knowing voter id
    [Required] public string BlindedCredential {get;set;} = string.Empty;

}