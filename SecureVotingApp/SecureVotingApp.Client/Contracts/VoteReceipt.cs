//@author Moïse
//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Client.Contracts;

//receipt to show client successful vote submission to server

public class VoteReceipt
{
    //identify the election the voter is voting in
     public string ElectionId {get; set;} = string.Empty;
   
    //receipt identifier
    public string ReceiptId {get; set;} = string.Empty; 

    //confirmation message
    public string ConfirmationMessage {get; set;} = string.Empty;
}
