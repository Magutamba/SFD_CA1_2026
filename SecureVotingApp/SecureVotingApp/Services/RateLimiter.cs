using System.Security.Claims;
using System.Threading.RateLimiting;

namespace SecureVotingApp.Services;

public static class RateLimiter
{
    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Rate Limiter HTTP code once tripped - HTTP Status Code 429 - purpose is to alert the client that the server will not accept additional requests
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            // Return message if rate limiting policy is reached
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json"; // Return type 
                
                // Returned Message
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests", 
                    Detail = "API rate limit exceeded. Please check your request frequency or back off temporarily.",
                    Instance = context.HttpContext.Request.Path
                }, cancellationToken);
            };
            
            // General Rule for following policies -
            // If an endpoint contains the attribute [Authorize] - it requires that the requester has been authorized via Login
            // These endpoints must use the [EnableRateLimiting("StandardAuthPolicy")] attribute
            
            // Anonymous endpoints that do not require authorized access should use [EnableRateLimiting("StrictAuthPolicy")]
            
            // Note: Controllers themselves can be given the Rate Limiting attribute if all endpoints are either anonymous or nonanonymous
            // If a controller has a mix of these endpoints each endpoint must contain the attribute definition based on its need
            
            // POLICY A: Standard Partitioned API Limiter
            // Explicitly for users that have been authenticated by the server
            // Rate Limiting policy in this case is associated to the HTTP Context's principal user
            // The user is found based on their name identifier - based on the current implementation in the auth controller this is the user's ID
            // If the user by ID could not be found - a redundant "AuthUser" name and their IP address is used
            options.AddPolicy("StandardApiPolicy", httpContext =>
            {
                // User details are taken from the server's HTTP Context to extract the user and use their details or IP address as the key for the rate limiter token bucket
                var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "AuthUser"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "AnonymousIP";
                
                // The rate limiter token bucket definition
                // Each user is given a maximum of 60 tokens to use at any given time
                // Each request reduces the token by 1
                // After a period of 60 seconds a total of 30 tokens are replenished into the token bucket to a maximum of 60
                // Queue limit stores excess requests once available tokens have been exhausted - requests are held until more tokens are maid available
                return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 60, // The total number of available tokens
                    QueueLimit = 0, // Defines the number of requests held after token limit has been reached - can be increased given business invariant
                    ReplenishmentPeriod = TimeSpan.FromSeconds(60), // Period of time until tokens can be added to bucket
                    TokensPerPeriod = 30, // On replenishment - 30 tokens are added to a maximum of token limit
                    AutoReplenishment = true // tokens are automatically replenished and does not require manual checking
                });
            });

            // POLICY B: Strict Auth Policy
            // Explicitly for users that have not been authenticated by the server
            // Rate Limiting policy is associated to the requester's IP address - if no such can be determined the IP address is defaulted to "AnonymousIP" and shares the same token limit as other unknown IP addresses
            options.AddPolicy("StrictAuthPolicy", httpContext =>
            {
                // Extract user's IP address for token key
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "AnonymousIP";
                
                // Rate limiter partition rules for anonymous requests
                // Each anonymous user is allowed a total of 5 requests per minute
                // Queue limit is set to 0 - no requests sent after rate limit is reached will be held by server
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, // Number of requests
                    Window = TimeSpan.FromMinutes(1), // Replenishment period
                    QueueLimit = 0 // number of held requests
                });
            });
        });

        return services;
    }
}