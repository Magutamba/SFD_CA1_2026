// File: AppDbContext.cs
// @uthor: Jeán Walton
// Date: 28/10/2026

using Microsoft.EntityFrameworkCore;
using SecureVotingApp.Models;

namespace SecureVotingApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!; //DbSet is a generic class provided by EFC(entity framework core) a set of (Users)
                                                    //@author Moïse records that a user has already received a credential for the election 
    public DbSet<IssuedCredential> IssuedCredentials => Set<IssuedCredential>();
    //@author Moïse records that an anonymous credential has been used once
    public DbSet<SpentCredential> SpentCredentials => Set<SpentCredential>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)//base passes the argument to the parent
    {

    }
    //@author Aaron Byrne
    public DbSet<AnonBallot> AnonBallots => Set<AnonBallot>();

    //enforcement that emails must be unique 
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
            .HasKey(ballot => ballot.Id);//Sets the ballot ID as the primary key

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

//this class incorperates class from EFC to basically access and edit the data base