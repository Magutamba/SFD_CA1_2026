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

    [HttpPost]
    public IActionResult IssueCredential(string electionId,CredentialRequest request)
    {
        //Authorize validate the login cookie
        string ?userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if(string.IsNullOrEmpty(userId)){
            return Unauthorized();
        }

        //check userId + electionId in the database to prevent second credential request
        //return 200 OK 
        return Ok();
    }
}