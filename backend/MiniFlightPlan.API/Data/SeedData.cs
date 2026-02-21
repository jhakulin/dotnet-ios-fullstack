// ============================================================
// Data/SeedData.cs — Initial data seeded into the database
//
// Seeding in EF Core can also be done via modelBuilder.Entity<T>().HasData()
// in OnModelCreating, but that approach ties seed data to migrations.
// This approach — checking and inserting at startup — is simpler for dev/learning.
// ============================================================

using MiniFlightPlan.API.Models;

namespace MiniFlightPlan.API.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        // Idempotent — only seed if no airports exist yet
        if (context.Airports.Any()) return;

        var airports = new[]
        {
            new Airport { IcaoCode = "KSEA", Name = "Seattle-Tacoma International Airport", City = "Seattle",     State = "WA" },
            new Airport { IcaoCode = "KPHX", Name = "Phoenix Sky Harbor International Airport", City = "Phoenix", State = "AZ" },
            new Airport { IcaoCode = "KORD", Name = "O'Hare International Airport",           City = "Chicago",  State = "IL" },
            new Airport { IcaoCode = "KLAX", Name = "Los Angeles International Airport",       City = "Los Angeles", State = "CA" },
            new Airport { IcaoCode = "KDEN", Name = "Denver International Airport",            City = "Denver",   State = "CO" },
            new Airport { IcaoCode = "KBOS", Name = "Boston Logan International Airport",      City = "Boston",   State = "MA" },
            new Airport { IcaoCode = "KATL", Name = "Hartsfield-Jackson Atlanta International", City = "Atlanta", State = "GA" },
            new Airport { IcaoCode = "KDFW", Name = "Dallas/Fort Worth International Airport", City = "Dallas",   State = "TX" },
            new Airport { IcaoCode = "KCHANDLER", Name = "Chandler Municipal Airport",         City = "Chandler", State = "AZ" },
            new Airport { IcaoCode = "KBFI", Name = "Boeing Field King County International",  City = "Seattle",  State = "WA" },
        };

        context.Airports.AddRange(airports);
        context.SaveChanges();

        // Seed a couple of sample flight plans for immediate testing
        var sea  = context.Airports.First(a => a.IcaoCode == "KSEA");
        var phx  = context.Airports.First(a => a.IcaoCode == "KPHX");
        var den  = context.Airports.First(a => a.IcaoCode == "KDEN");

        var flightPlans = new[]
        {
            new FlightPlan
            {
                AircraftRegistration = "N12345",
                DepartureAirportId = sea.Id,
                ArrivalAirportId = phx.Id,
                EstimatedDepartureTime = DateTime.UtcNow.AddHours(2),
                EteMinutes = 155,
                Route = "KSEA SEA J80 LKV J80 LAS KPHX",
                FlightRules = FlightRules.IFR,
                Status = FlightStatus.Filed,
                FiledAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new FlightPlan
            {
                AircraftRegistration = "N67890",
                DepartureAirportId = phx.Id,
                ArrivalAirportId = den.Id,
                EstimatedDepartureTime = DateTime.UtcNow.AddHours(5),
                EteMinutes = 100,
                Route = "KPHX GBN J48 PUB KDEN",
                FlightRules = FlightRules.IFR,
                Status = FlightStatus.Draft,
            }
        };

        context.FlightPlans.AddRange(flightPlans);
        context.SaveChanges();
    }
}
