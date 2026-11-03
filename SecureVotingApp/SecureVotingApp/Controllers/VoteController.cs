//@author Moïse

//namespace for grouping services components together
namespace SecureVotingApp.Controllers;

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
        //implementation for submitting the vote to the server, validating the anonymous credential, and recording the vote in the election database
        //temporary
        //validate the request model
        return Ok(new VoteReceipt
        {
           ElectionId = request.ElectionId,
           ReceiptId = Guid.NewGuid().ToString(),
           ConfirmationMessage = "Your vote has been successfully submitted."
        });
    }
}