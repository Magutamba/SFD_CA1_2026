//@author Moïse
//logical namespace for grouping services components together
namespace SecureVotingApp.Client.Services;

//send blinded credentialpayload to server
public class BlindCredentialRequest
{
    //blinded credential
    public string BlindedCredential { get; set; } = string.Empty;
    //kept in browser only to unblind
    public BlindState State { get; set; } = new BlindState();
}

//kept in browser memory only, never sent to the server
public class BlindState
{
    //store 32 byte randomized prepared message prefix required by RFC 9474
    public string MessageRandomizer {get;set;} = string.Empty;
    //temporary secret value required to unblind the signed blinded credential
    public string BlindingValue {get;set;} = string.Empty;
    //electionId to track
    public string ElectionId {get;set;} = string.Empty;
}