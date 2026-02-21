// ============================================================
// Services/FlightPlanService.cs — The Service Implementation
//
// This is where all the business logic and data access lives.
// Key concepts demonstrated:
//   - Constructor injection of DbContext
//   - LINQ queries against EF Core DbSets
//   - .Include() for eager loading related entities
//   - async/await throughout (never block with .Result or .Wait())
//   - Mapping between Entity <-> DTO
//   - Business rule enforcement (e.g. status transition rules)
// ============================================================

using Microsoft.EntityFrameworkCore;
using MiniFlightPlan.API.Data;
using MiniFlightPlan.API.DTOs;
using MiniFlightPlan.API.Models;

namespace MiniFlightPlan.API.Services;

public class FlightPlanService : IFlightPlanService
{
    private readonly AppDbContext _db;

    // CONSTRUCTOR INJECTION:
    // ASP.NET Core's DI container sees that this constructor needs an AppDbContext.
    // It was registered as Scoped in Program.cs, so one instance is created
    // per HTTP request and injected here automatically.
    public FlightPlanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FlightPlanResponse>> GetAllAsync(string? statusFilter = null)
    {
        // Start building the LINQ query — nothing hits the DB yet at this point
        // IQueryable<T> is deferred — EF Core builds the SQL as you chain methods
        var query = _db.FlightPlans
            // .Include() = JOIN — tells EF to load the related Airport entities
            // Without this, DepartureAirport would be null (lazy loading is off by default)
            .Include(fp => fp.DepartureAirport)
            .Include(fp => fp.ArrivalAirport)
            .AsQueryable(); // ensures we're working with IQueryable for optional chaining

        // Conditionally add a WHERE clause — the filter is only applied if provided
        // This is a common LINQ pattern: build the query dynamically
        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<FlightStatus>(statusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(fp => fp.Status == status);
        }

        // Order by departure time ascending
        // .OrderBy() translates to ORDER BY EstimatedDepartureTime ASC in SQL
        query = query.OrderBy(fp => fp.EstimatedDepartureTime);

        // .ToListAsync() — this is where the SQL is actually executed
        // Always use async DB operations — never block with .ToList() on a web server
        var flightPlans = await query.ToListAsync();

        // Map Entity → DTO using LINQ .Select()
        // Select() here is a projection — transform each entity to a response DTO
        return flightPlans.Select(MapToResponse).ToList();
    }

    public async Task<FlightPlanResponse?> GetByIdAsync(int id)
    {
        // .FirstOrDefaultAsync() returns null if not found — safe for single lookups
        // Always include related entities needed for the DTO mapping
        var flightPlan = await _db.FlightPlans
            .Include(fp => fp.DepartureAirport)
            .Include(fp => fp.ArrivalAirport)
            .FirstOrDefaultAsync(fp => fp.Id == id);

        // Null-conditional: if not found, return null (controller will return 404)
        return flightPlan == null ? null : MapToResponse(flightPlan);
    }

    public async Task<FlightPlanResponse> CreateAsync(CreateFlightPlanRequest request)
    {
        // Business rule: can't depart and arrive at the same airport
        if (request.DepartureAirportId == request.ArrivalAirportId)
            throw new InvalidOperationException("Departure and arrival airports must be different.");

        // Business rule: can't schedule a flight in the past
        if (request.EstimatedDepartureTime < DateTime.UtcNow)
            throw new InvalidOperationException("Estimated departure time must be in the future.");

        // Map request DTO → Entity
        var flightPlan = new FlightPlan
        {
            AircraftRegistration = request.AircraftRegistration,
            DepartureAirportId = request.DepartureAirportId,
            ArrivalAirportId = request.ArrivalAirportId,
            EstimatedDepartureTime = request.EstimatedDepartureTime,
            EteMinutes = request.EteMinutes,
            Route = request.Route,
            FlightRules = request.FlightRules,
            Status = FlightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        // .Add() tells EF to track this new entity (INSERT on SaveChanges)
        _db.FlightPlans.Add(flightPlan);

        // SaveChangesAsync() executes the INSERT and populates the generated Id
        await _db.SaveChangesAsync();

        // Reload with navigation properties for the response DTO
        await _db.Entry(flightPlan).Reference(fp => fp.DepartureAirport).LoadAsync();
        await _db.Entry(flightPlan).Reference(fp => fp.ArrivalAirport).LoadAsync();

        return MapToResponse(flightPlan);
    }

    public async Task<FlightPlanResponse?> UpdateStatusAsync(int id, UpdateFlightPlanStatusRequest request)
    {
        var flightPlan = await _db.FlightPlans
            .Include(fp => fp.DepartureAirport)
            .Include(fp => fp.ArrivalAirport)
            .FirstOrDefaultAsync(fp => fp.Id == id);

        if (flightPlan == null) return null;

        // Business rule: validate status transitions
        // (e.g. you can't go from Closed back to Active)
        ValidateStatusTransition(flightPlan.Status, request.NewStatus);

        flightPlan.Status = request.NewStatus;
        flightPlan.UpdatedAt = DateTime.UtcNow;

        // Set FiledAt timestamp when transitioning to Filed
        if (request.NewStatus == FlightStatus.Filed)
            flightPlan.FiledAt = DateTime.UtcNow;

        // EF Core change tracking — it knows this entity was modified,
        // so SaveChanges generates an UPDATE (not INSERT)
        await _db.SaveChangesAsync();

        return MapToResponse(flightPlan);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var flightPlan = await _db.FlightPlans.FindAsync(id);
        if (flightPlan == null) return false;

        // Only allow deletion of Draft or Cancelled flight plans
        if (flightPlan.Status != FlightStatus.Draft && flightPlan.Status != FlightStatus.Cancelled)
            throw new InvalidOperationException($"Cannot delete a flight plan with status '{flightPlan.Status}'.");

        _db.FlightPlans.Remove(flightPlan);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── PRIVATE HELPERS ──────────────────────────────────────

    // Mapping method — keeps DTO mapping logic in one place
    // In larger projects you'd use AutoMapper for this, but explicit
    // mapping is clearer for learning
    private static FlightPlanResponse MapToResponse(FlightPlan fp) => new()
    {
        Id = fp.Id,
        AircraftRegistration = fp.AircraftRegistration,
        DepartureIcao = fp.DepartureAirport?.IcaoCode ?? "Unknown",
        DepartureAirportName = fp.DepartureAirport?.Name ?? "Unknown",
        ArrivalIcao = fp.ArrivalAirport?.IcaoCode ?? "Unknown",
        ArrivalAirportName = fp.ArrivalAirport?.Name ?? "Unknown",
        EstimatedDepartureTime = fp.EstimatedDepartureTime,
        EteMinutes = fp.EteMinutes,
        Route = fp.Route,
        FlightRules = fp.FlightRules.ToString(),
        Status = fp.Status.ToString(),
        CreatedAt = fp.CreatedAt,
        FiledAt = fp.FiledAt
    };

    private static void ValidateStatusTransition(FlightStatus current, FlightStatus next)
    {
        // Define valid transitions — in a real system this would be more sophisticated
        var validTransitions = new Dictionary<FlightStatus, FlightStatus[]>
        {
            [FlightStatus.Draft]     = [FlightStatus.Filed, FlightStatus.Cancelled],
            [FlightStatus.Filed]     = [FlightStatus.Active, FlightStatus.Cancelled],
            [FlightStatus.Active]    = [FlightStatus.Closed, FlightStatus.Cancelled],
            [FlightStatus.Closed]    = [],
            [FlightStatus.Cancelled] = [],
        };

        if (!validTransitions[current].Contains(next))
            throw new InvalidOperationException(
                $"Invalid status transition from '{current}' to '{next}'.");
    }
}
