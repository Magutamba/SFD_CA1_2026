//@author Moïse

//designates class as Api controller automatical model validation and Http response behaviours
[ApiController]

//allow anonymous access to the controller, so voter can submit ballot anonymously
[AllowAnonymous]

//route for the votecontroller 
[Route("api/votes")]

//class for handling vote submission requests from the client
public class VoteController : ControllerBase
{
    //handle post request to submit ballot
    [HttpPost]
    public IActionResult SubmitVote(AnonymousVoteSubmit request)
    {
        //validate the request model
        return Ok(new
        {
            status = "Accepted"
        });
    }
}