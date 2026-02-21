// ============================================================
// Models/FlightPlan.cs — EF Core Entity
// ============================================================

namespace MiniFlightPlan.API.Models;

// Enum stored as int in the database by default
// This is a common pattern — maps to a small, fixed set of states
public enum FlightStatus
{
    Draft = 0,
    Filed = 1,
    Active = 2,
    Closed = 3,
    Cancelled = 4
}

public enum FlightRules
{
    VFR = 0,  // Visual Flight Rules — pilot navigates by sight
    IFR = 1   // Instrument Flight Rules — pilot navigates by instruments, requires filing
}

public class FlightPlan
{
    public int Id { get; set; }

    // Aircraft registration number — e.g. N12345 (US), C-FABC (Canada)
    public required string AircraftRegistration { get; set; }

    // ── FOREIGN KEYS + NAVIGATION PROPERTIES ────────────────
    // EF Core convention: [EntityName]Id = foreign key
    // EF Core convention: [EntityName] = navigation property to the related entity
    // Together, these let EF join the tables and load related data with .Include()

    public int DepartureAirportId { get; set; }
    public Airport? DepartureAirport { get; set; }  // loaded via .Include(fp => fp.DepartureAirport)

    public int ArrivalAirportId { get; set; }
    public Airport? ArrivalAirport { get; set; }

    public DateTime EstimatedDepartureTime { get; set; }

    // Estimated Time En Route in minutes
    public int EteMinutes { get; set; }

    // Route string — like a path through waypoints, e.g. "KSEA DCT PDT V23 BOI"
    public string? Route { get; set; }

    public FlightRules FlightRules { get; set; } = FlightRules.VFR;
    public FlightStatus Status { get; set; } = FlightStatus.Draft;

    // Audit fields — standard practice on any production entity
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? FiledAt { get; set; }  // set when status transitions to Filed
}
