//@author Aaron Byrne
// 4/11/2026


namespace SecureVotingApp.Models;

public class AnonBallot
{
    public int Id {get;set;}//Used as a primary key by for each ballot ONLY
    public String VoteChoice { get; set; } = string.Empty;//Stores the vote selected by the user
    public String ElectionId { get; set; } = string.Empty;//Identifies which election the ballot belongs to
}