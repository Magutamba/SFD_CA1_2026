// File: AppDbContext.cs
// @uthor: Jeán Walton
// Date: 28/10/2026

using Microsoft.EntityFrameworkCore;

namespace SecureVotingApp.Data;

public class AppDbContext : DbContext
{
   public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)//base passes the argument to the parent
    {
        
    } 
}