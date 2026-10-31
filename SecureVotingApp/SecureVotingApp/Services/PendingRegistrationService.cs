// File: PendingRegistrationService.cs
// @uthor: Jeán Walton
// 31/10/2026

using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using SecureVotingApp.Models;

namespace SecureVotingApp.Services;

public class PendingRegistrationService
{
    private readonly IMemoryCache _cache;

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
    /*  RandomNumberGenerator.GetBytes(32) generates 32 unpredictable bytes.
        Convert.ToBase64String() converts them into a string the frontend can send back.
        _cache.Set() stores your PendingRegistration object under that reference.
        TimeSpan.FromMinutes(10) automatically expires the entry after 10 minutes.
        The method returns the reference, not the registration details or TOTP secret.
    */

    // Retrieve a pending registration using its temporary reference.
    public PendingRegistration? GetPendingRegistration(string registrationId)
    {
        return _cache.Get<PendingRegistration>(registrationId);
    }

    /*  How it works
        registrationId is the random reference generated earlier.
        _cache.Get<PendingRegistration>() looks for that reference in memory.
        If it exists, the method returns the pending registration details.
        If it has expired or doesn't exist, the method returns null.
        The ? means the method can return either a PendingRegistration object or null.
    */

    // Remove temporary registration details after successful 2FA verification.
    public void RemovePendingRegistration(string registrationId)
    {
        _cache.Remove(registrationId);
    }

    // Record an invalid TOTP attempt and cancel registration after 5 failures.
    public bool RecordFailedAttempt(string registrationId)
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