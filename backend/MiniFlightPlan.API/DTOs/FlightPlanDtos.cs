// ============================================================
// DTOs/FlightPlanDtos.cs — Data Transfer Objects
//
// DTOs are the shapes of data that cross the API boundary.
// They differ from Entities because:
//   - Response DTOs flatten related entities (embed airport codes
//     directly instead of returning the whole Airport object)
//   - Request DTOs contain only what the caller provides
//     (no Id, no CreatedAt — the server sets those)
//   - Data annotations here drive validation before the
//     controller even runs (ASP.NET Core does this automatically
//     for [ApiController] decorated controllers)
// ============================================================

using System.ComponentModel.DataAnnotations;
using MiniFlightPlan.API.Models;

namespace MiniFlightPlan.API.DTOs;

// ── RESPONSE DTO ─────────────────────────────────────────────
// What the API returns when a caller asks for a flight plan.
// Notice it has airport ICAO codes directly — the caller doesn't
// need the full Airport entity, just the identifier.

public class FlightPlanResponse
{
    public int Id { get; set; }
    public required string AircraftRegistration { get; set; }

    // Flattened from the navigation properties — no nested Airport object
    public required string DepartureIcao { get; set; }
    public required string DepartureAirportName { get; set; }
    public required string ArrivalIcao { get; set; }
    public required string ArrivalAirportName { get; set; }

    public DateTime EstimatedDepartureTime { get; set; }
    public int EteMinutes { get; set; }
    public string? Route { get; set; }
    public string FlightRules { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? FiledAt { get; set; }
}

// ── CREATE REQUEST DTO ───────────────────────────────────────
// What the caller must provide to create a new flight plan.
// Data annotations drive automatic validation:
//   [Required]    → returns 400 Bad Request if missing
//   [MaxLength]   → returns 400 if too long
//   [Range]       → returns 400 if out of range

public class CreateFlightPlanRequest
{
    [Required]
    [StringLength(10, MinimumLength = 3, ErrorMessage = "Aircraft registration must be 3-10 characters")]
    public required string AircraftRegistration { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DepartureAirportId must be a valid airport ID")]
    public int DepartureAirportId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ArrivalAirportId must be a valid airport ID")]
    public int ArrivalAirportId { get; set; }

    [Required]
    public DateTime EstimatedDepartureTime { get; set; }

    [Range(1, 1440, ErrorMessage = "ETE must be between 1 and 1440 minutes (24 hours)")]
    public int EteMinutes { get; set; }

    [StringLength(500)]
    public string? Route { get; set; }

    public FlightRules FlightRules { get; set; } = FlightRules.VFR;
}

// ── UPDATE STATUS REQUEST DTO ────────────────────────────────
// Separate DTO for status transitions — a common pattern.
// You wouldn't want callers to change any field via a general
// update; status changes often have business rules attached.

public class UpdateFlightPlanStatusRequest
{
    [Required]
    public FlightStatus NewStatus { get; set; }
}

// ── AIRPORT RESPONSE DTO ─────────────────────────────────────
public class AirportResponse
{
    public int Id { get; set; }
    public required string IcaoCode { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
}
