//@author Moïse
//namespace for grouping models components together
namespace SecureVotingApp.Models;

//server controlled lif cycle of a credential, used to track whether a credential has been issued or spent

public enum CredentialStatus
{
    //reservation of a credential exists but hasn't been used
    Pending = 0,
    //reservation of a credential exists as be used
    Issued = 1

}