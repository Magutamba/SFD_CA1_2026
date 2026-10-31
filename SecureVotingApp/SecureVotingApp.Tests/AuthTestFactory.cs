// File: AuthTestFactory.cs
// @uthor: Jeán Walton
// 30/10/2026

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SecureVotingApp.Data;



public class AuthTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _sqlitConnection;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
           //remove the applications existing database configuration 
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            //Register AppDbContext using our temporary SQLite connection
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_sqlitConnection)); 
        });
    }

    public AuthTestFactory()
    {   //creates a SQLite connection "Data Source=:memory:" creates a tempory database
        _sqlitConnection = new SqliteConnection("Data Source=:memory:");
        _sqlitConnection.Open();
    }

    public void InitialiseDatabase()
    {
        //starts the test application
        using var client = CreateClient();

        //create a dependency injection scope
        using var scope = Services.CreateScope();

        //retrieves the test database context
        var tempDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        //creates the tables including the Users
        tempDb.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sqlitConnection.Dispose();
        }
            
        base.Dispose(disposing);
    }

}