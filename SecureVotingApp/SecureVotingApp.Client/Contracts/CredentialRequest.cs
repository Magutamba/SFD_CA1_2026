

//contract for requesting ballot from server in order to vote
public class CredentialRequest
{
    [Required] [voterId] public string VoterId {get; set;} = string.Empty;

    [Required] [credential]  public string Credential {get;set;} = string.Empty;


}