// ============================================================
// CustomWebApplicationFactory
//
// Spins up the real ASP.NET Core pipeline in-process for
// integration tests — no actual HTTP, no real server needed.
//
// We override the database configuration to swap SQLite for
// an EF Core InMemory database so tests are:
//   - Fast (no disk I/O)
//   - Isolated (fresh DB name per factory instance)
//   - Portable (no SQLite file left behind)
//
// ConfigureTestServices runs AFTER the app's own service
// registrations, so our InMemory registration wins.
// Program.cs startup code (EnsureCreated + SeedData) then
// runs against the InMemory database automatically.
// ============================================================

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MiniFlightPlan.API.Data;

namespace MiniFlightPlan.Tests.Integration;

// IClassFixture tells xUnit to create one factory for all tests in this class
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique DB name per factory instance — keeps test classes isolated
    private readonly string _dbName = Guid.NewGuid().ToString();

    // A standalone EF Core internal service provider that only knows about the
    // InMemory provider.  By passing this to UseInternalServiceProvider we tell
    // EF Core "don't build your internals from the app DI container" — which
    // prevents the "two providers registered" error that occurs because Program.cs
    // calls UseSqlite() first (which registers SQLite infrastructure services into
    // the app container) and our test code then also calls UseInMemoryDatabase().
    private static readonly IServiceProvider _inMemoryEfProvider =
        new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ConfigureTestServices runs AFTER the app registers its own services,
        // which means our registration overrides the SQLite one from Program.cs.
        builder.ConfigureTestServices(services =>
        {
            // Remove the DbContextOptions<AppDbContext> and AppDbContext registrations
            // that Program.cs created for SQLite.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            // Register a fresh AppDbContext backed by an InMemory database.
            // UseInternalServiceProvider points EF at our isolated provider so it
            // never sees the SQLite infrastructure that is still sitting in the app
            // service collection — eliminating the dual-provider conflict.
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                       .UseInternalServiceProvider(_inMemoryEfProvider));
        });
    }
}
