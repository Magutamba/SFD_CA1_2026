//Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Components.Contracts;

//contract for credential response from server
public class CredentialResponse
{
    //signed credential issued by server after checking voter is logged in and hasn't voted
    public string SignedCredential {get;set;} =string.Empty;

    //candidates for the election
    public List<string> Candidates {get;set;} = new List<string>()
}   
