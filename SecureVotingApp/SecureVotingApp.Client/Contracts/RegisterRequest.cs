//import data annotations for validation e.g Required, EmailAddress, MinLength
using System.ComponentModel.DataAnnotations;
//logical namespace to group components contracts together prevents class name collisions
namespace SecureVotingApp.Components.Contracts;
public class RegisterRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required] [MinLength(12)] public string Password { get; set; } = string.Empty;
}