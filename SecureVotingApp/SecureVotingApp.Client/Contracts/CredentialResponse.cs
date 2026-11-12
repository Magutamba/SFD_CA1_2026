//@author Moïse 
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Client.Contracts;

//contract for credential response from server
public class CredentialResponse
{
    [Required]
    //identify the election the voter is voting in
    public string ElectionId { get; set; } = string.Empty;

    [Required]
    //signed blindend credential issued by server after checking voter is logged in and hasn't voted
    public string SignedBlindedCredential { get; set; } = string.Empty;
    // encrypted recovery package blind state
    public string RecoveryPackage { get; set; } = string.Empty;

    //yes no vote choices can allow spoiled ballots
    public List<string> Choices { get; set; } = new() { "Yes", " No" };

}
