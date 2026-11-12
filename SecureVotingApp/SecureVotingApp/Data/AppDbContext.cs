// File: AppDbContext.cs
// @uthor: Jeán Walton
// Date: 28/10/2026

using Microsoft.EntityFrameworkCore;
using SecureVotingApp.Models;

namespace SecureVotingApp.Data;

public class AppDbContext : DbContext
{

   public DbSet<User> Users { get; set; } = null!; //represents the Users table in the database

   //Passes the database configuration options to the DbContext base class
   public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    //@author Aaron Byrne
    public DbSet<AnonBallot> AnonBallots => Set<AnonBallot>();

    //creates a unique index to prevent duplicate emails addresses in SQLite
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>()
                    .HasIndex(user => user.Email)
                    .IsUnique();
        //@author Moïse a user can only receive one credential for the same election, unique prevent concurrency issues
        modelBuilder.Entity<IssuedCredential>()
                    .HasIndex(credential => new
                    {
                        credential.UserId,
                        credential.ElectionId
                    })
                    .IsUnique();

        // @author Moïse an anonymous credential can only be spent once during the election, unique to prevent concurrency issues
        modelBuilder.Entity<SpentCredential>()
                    .HasIndex(credential => new
                    {
                        credential.ElectionId,
                        credential.CredentialHash
                    })
                    .IsUnique();

        // @author Aaron Byrne
        // Configuring the AnonBallot storage for the database
        modelBuilder.Entity<AnonBallot>()
            .HasKey(ballot => ballot.Id);//Sets the ballot ID as the primary key for the AnonBallots table

        modelBuilder.Entity<AnonBallot>()
            .Property(ballot => ballot.VoteChoice)
            .IsRequired();//Ensures that there is a vote therefore vote cannot be null

        modelBuilder.Entity<AnonBallot>()
            .Property(ballot => ballot.ElectionId)
            .IsRequired();//Ensures that each vote belongs to an election
        //@author Moïse recovery package is unique to prevent concurrency issues
        modelBuilder.Entity<IssuedCredential>()
            .HasIndex(ic => ic.RecoveryPackage);// addemove unique before migration
    }

}
