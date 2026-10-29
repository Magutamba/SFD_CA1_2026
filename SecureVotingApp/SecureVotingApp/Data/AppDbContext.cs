// File: AppDbContext.cs
// @uthor: Jeán Walton
// Date: 28/10/2026

using Microsoft.EntityFrameworkCore;
using SecureVotingApp.Models;

namespace SecureVotingApp.Data;

public class AppDbContext : DbContext
{
   public DbSet<User> Users { get; set; } = null!; //DbSet is a generic class provided by EFC(entity framework core) a set of (Users)
   public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)//base passes the argument to the parent
    {
        
    }

    //enforcement that emails must be unique 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>()
                    .HasIndex(user => user.Email)
                    .IsUnique();
    }
    
}

//this class incorperates class from EFC to basically access and edit the data base