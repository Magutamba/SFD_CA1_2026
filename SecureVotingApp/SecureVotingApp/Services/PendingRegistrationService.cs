// File: PendingRegistrationService.cs
// @uthor: Jeán Walton
// 31/10/2026

using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using SecureVotingApp.Models;

namespace SecureVotingApp.Services;

public class PendingRegistrationService
{
    private readonly IMemoryCache _cache; //IMemoryCache supports concurrent access

    private readonly object _registrationLock = new();//lock: prevents two or more requests from modifyying the same shared data at the same time.

    public PendingRegistrationService(IMemoryCache cache)
    {
        _cache = cache;
    }

    // Store registration details temporarily until 2FA setup is confirmed.
    public string CreatePendingRegistration(PendingRegistration registration)
    {   

        // Generate a cryptographically secure, random reference.
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
        string registrationId = Convert.ToBase64String(randomBytes);

        // Store registration for a maximum of 10 minutes.
        _cache.Set(
            registrationId,
            registration,
            TimeSpan.FromMinutes(10)
        );

        return registrationId;
    }
   
    // Retrieve a pending registration using its temporary reference.
    public PendingRegistration? GetPendingRegistration(string registrationId)
    {
        return _cache.Get<PendingRegistration>(registrationId);
    }

    // Remove temporary registration details after successful 2FA verification.
    public void RemovePendingRegistration(string registrationId)
    {
        _cache.Remove(registrationId);
    }

    // Record an invalid TOTP attempt and cancel registration after 5 failures.
    public bool RecordFailedAttempt(string registrationId)
    {   
        //lock: prevents two or more requests from modifyying the same shared data at the same time.
        lock (_registrationLock) 
        {
             var pending = GetPendingRegistration(registrationId);

            if (pending is null)
            {
                return false;
            }

            pending.FailedAttempts++;

            if (pending.FailedAttempts >= 5)
            {
                RemovePendingRegistration(registrationId);
                return false;
            }

            return true;
        }
       
    }
}