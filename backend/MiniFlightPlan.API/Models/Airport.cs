// ============================================================
// Models/Airport.cs — EF Core Entity
//
// ENTITY vs DTO:
//   An Entity maps directly to a database table via EF Core.
//   It represents the stored data structure.
//   DTOs (in the DTOs/ folder) are what you expose over the API —
//   they can be shaped differently for input vs output.
// ============================================================

namespace MiniFlightPlan.API.Models;

public class Airport
{
    public int Id { get; set; }

    // ICAO code — the 4-letter identifier used in flight plans (e.g. KSEA, KPHX)
    // Required = not nullable in C# 8+ with nullable enabled
    public required string IcaoCode { get; set; }

    public required string Name { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }

    // EF Core navigation property — "one airport has many departure flight plans"
    // EF uses this to build the foreign key relationship in the database
    // The '?' means this collection isn't always loaded (lazy/explicit loading)
    public ICollection<FlightPlan>? DepartureFlightPlans { get; set; }
    public ICollection<FlightPlan>? ArrivalFlightPlans { get; set; }
}
