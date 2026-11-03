//@author Moïse
// provides the MVC controller and API controller ([ApiController], HTTP request handlers)
using Microsoft.AspNetCore.Mvc;
//provides authorization attributes for restricting access to authenticated users
using Microsoft.AspNetCore.Authorization;
//provides access to identity claims to retrieve the logged in userId
using System.Security.Claims;
//import the custom contract for credential request and response
using SecureVotingApp.Components.Contracts;
//designates class as Api controller automatical model validation and Http response behaviours
[ApiController]

//restricts access to authenticated users only
[Authorize]

//route for the controller 
[Route("api/elections/{electionId}/credential")]

//class for handling credential requests from the client
public class CredentialController : ControllerBase
{
    //inject the credential service to handle signing of blinded credentials
    private readonly CredentialService credentialService;

    //inject the credential service to the controller via dependency injection
    public CredentialController(CredentialService credentialService)
    {
        this.credentialService = credentialService;
    }

    //handle post request to issue a signed credential for the voter, FromBody attribute indicates the request body will be deserialized into the CredentialRequest object
    [HttpPost]
    public async Task<IActionResult> IssueCredential(string electionId,[FromBody]CredentialRequest request)
    {
        //Authorize validate the login cookie
        string ?userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if(string.IsNullOrEmpty(userId)){
            return Unauthorized();
        }

        try
        {
            //request credential service to sign blinded credential
            CredentialResponse result = await credentialService.issueAsync(userId,electionId,request);

            return Ok(result);
        }catch(Exception e)
        {
            //return conflict if the user already received a credential for this election
            return conflict(new {message = e.getMessage});
        }
    }
}