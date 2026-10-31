//@author Moïse 
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Components.Contracts;

//contract for credential response from server
public class CredentialResponse
{
    //identify the election the voter is voting in
     public string ElectionId {get; set;} = string.Empty;

    //signed blindend credential issued by server after checking voter is logged in and hasn't voted
    public string SignedBlindedCredential {get;set;} =string.Empty;

    //candidates for the election
    public List<string> Candidates {get;set;} = new List<string>();
    //yes no vote, will pick which later
    public List<string> YesNoChoices {get;set;} = new List<string>();

}   
