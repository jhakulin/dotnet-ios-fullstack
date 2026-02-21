// ============================================================
// Data/AppDbContext.cs — The EF Core Database Context
//
// DbContext is the central class in EF Core. It:
//   1. Represents a session with the database
//   2. Exposes DbSet<T> properties — one per table
//   3. Tracks changes to entities in memory
//   4. Translates LINQ queries to SQL via SaveChangesAsync()
//
// Think of it as EF's unit of work + repository combined.
// It's registered as Scoped in Program.cs — one instance per HTTP request.
// ============================================================

using Microsoft.EntityFrameworkCore;
using MiniFlightPlan.API.Models;

namespace MiniFlightPlan.API.Data;

public class AppDbContext : DbContext
{
    // Constructor injection — DbContextOptions comes from Program.cs registration
    // (the SQLite connection string, etc.)
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DB SETS = DATABASE TABLES ────────────────────────────
    // DbSet<T> represents a table. You query it with LINQ:
    //   context.FlightPlans.Where(fp => fp.Status == FlightStatus.Filed).ToListAsync()
    // EF translates that LINQ to: SELECT * FROM FlightPlans WHERE Status = 1

    public DbSet<FlightPlan> FlightPlans => Set<FlightPlan>();
    public DbSet<Airport> Airports => Set<Airport>();

    // ── MODEL CONFIGURATION ──────────────────────────────────
    // OnModelCreating is where you configure things EF can't infer by convention
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Airport entity
        modelBuilder.Entity<Airport>(entity =>
        {
            // Unique index on ICAO code — no two airports with same code
            entity.HasIndex(a => a.IcaoCode).IsUnique();

            // Required fields mapped to NOT NULL columns
            entity.Property(a => a.IcaoCode).IsRequired().HasMaxLength(4);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.Property(a => a.City).IsRequired().HasMaxLength(100);
            entity.Property(a => a.State).IsRequired().HasMaxLength(50);
        });

        // Configure FlightPlan entity
        modelBuilder.Entity<FlightPlan>(entity =>
        {
            entity.Property(fp => fp.AircraftRegistration)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(fp => fp.Route).HasMaxLength(500);

            // Store enums as strings in the database for readability
            // Without this they'd be stored as integers (0, 1, 2...)
            entity.Property(fp => fp.Status)
                  .HasConversion<string>();
            entity.Property(fp => fp.FlightRules)
                  .HasConversion<string>();

            // ── RELATIONSHIP CONFIGURATION ───────────────────
            // EF needs to know about both relationships to the Airport table
            // (departure AND arrival) — otherwise it can't build the correct FK columns

            entity.HasOne(fp => fp.DepartureAirport)
                  .WithMany(a => a.DepartureFlightPlans)
                  .HasForeignKey(fp => fp.DepartureAirportId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't cascade-delete flight plans

            entity.HasOne(fp => fp.ArrivalAirport)
                  .WithMany(a => a.ArrivalFlightPlans)
                  .HasForeignKey(fp => fp.ArrivalAirportId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index on status for efficient querying by status
            entity.HasIndex(fp => fp.Status);
            entity.HasIndex(fp => fp.EstimatedDepartureTime);
        });
    }
}
