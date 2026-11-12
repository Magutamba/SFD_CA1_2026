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

    //creates a unique index to prevent duplicate emails addresses in SQLite
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>()
                    .HasIndex(user => user.Email)
                    .IsUnique();
    }
    
}
