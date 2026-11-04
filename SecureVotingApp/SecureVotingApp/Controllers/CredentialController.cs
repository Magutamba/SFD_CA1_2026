//@author Moïse
// provides the MVC controller and API controller ([ApiController], HTTP request handlers)
using Microsoft.AspNetCore.Mvc;

//provides authorization attributes for restricting access to authenticated users
using Microsoft.AspNetCore.Authorization;

//provides access to identity claims to retrieve the logged in userId
using System.Security.Claims;

//import the custom contract for credential request and response
using SecureVotingApp.Client.Contracts;//might need to change to a shared contract folder
//
using SecureVotingApp.Services;

//namespace for grouping services components together
namespace SecureVotingApp.Controllers;

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
        string ?userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        //if the userId claim is missing or invalid return unauthorized
        if(!int.TryParse(userIdValue, out int userId))
        {
            return Unauthorized();
        }

        try
        {
            //request credential service to sign blinded credential
            CredentialResponse result = await credentialService.IssueAsync(userId,electionId,request);

            return Ok(result);
        }catch(Exception e)
        {
            //return conflict if the user already received a credential for this election
            return Conflict(new {message = e.Message});
        }
    }
}