//@author Moïse
//designates class as Api controller automatical model validation and Http response behaviours
[ApiController]

//restricts access to authenticated users only
[Authorize]

//route for the controller 
[Route("api/elections/{electionId}/credential")]

//class for handling credential requests from the client
public class CredentialController : ControllerBase
{
    [HttpPost]
    public IActionResult IssueCredential(string electionId,CredentialRequest request)
    {
        //Authorize validate the login cookie
        string ?userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if(userId == null){
            return Unauthorized();
        }

        //check userId + electionId in the database to prevent second credential request
        //return 200 OK 
        return Ok();
    }
}