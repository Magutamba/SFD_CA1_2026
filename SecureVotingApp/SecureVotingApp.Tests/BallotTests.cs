//Author Aaron Byrne
//Automatic testing of casting ballots

using System.Net;//Needed for HTTP codes
using System.Net.Http.Json;//Needed for sending JSON data to the api 
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace secureVotingApp.Tests;
public class BallotTests
{
    [Fact]
    public async Task SubmitInvalidBallot()
    {
        //Arrange-Setting up what's required for the test

        using var factory = new AuthTestFactory();//Create a temporary test application
        factory.InitialiseDatabase();//Set up the temporary database

        using var client = factory.CreateClient(//Create client to send requests to the application
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new  Uri("https://localhost")//Ensuring HTTPS is used for tests
            });
        var ballot = new
        {
            ElectionId = "election-2026",
            VoteChoice = "applesauce",//As this is neither "Yes" or "No", it is an invalid vote, which is the purpose of this test
            AnonymousCredential = "AB",
            MessageRandomizer = "AAAHAHAHAHAHAHAAHHAHAHA"
        };
        
        //Act - Perform the action of casting the vote
        var response = await client.PostAsJsonAsync("/api/vote", ballot);
        
        //Assert = Verify that the vote gets rejected
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}