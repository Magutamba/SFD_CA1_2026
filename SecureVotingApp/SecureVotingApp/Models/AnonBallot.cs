//@author Aaron Byrne
// 4/11/2026


namespace SecureVotingApp.Models;

public class AnonBallot
{
    public int Id {get;set;}//Assigns an ID to each vote for tracking purposes. Used as a primary key by EF and has no ties to voters
    public String VoteChoice { get; set; } = string.Empty;//Stores the vote selected by the user
    public String ElectionId { get; set; } = string.Empty;//Identifies which election the ballot belongs to
}