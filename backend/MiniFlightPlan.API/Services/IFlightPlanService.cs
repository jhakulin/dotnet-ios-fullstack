// ============================================================
// Services/IFlightPlanService.cs — The Service Interface
//
// WHY DEFINE AN INTERFACE?
//   1. Controllers depend on IFlightPlanService, not FlightPlanService.
//      This is the Dependency Inversion Principle.
//   2. In tests, you can inject a mock IFlightPlanService — the
//      controller doesn't know or care it's a fake.
//   3. If you ever need a different implementation (e.g. a caching
//      decorator, or a version that calls an external API instead
//      of the DB), you just swap the registration in Program.cs.
//
// This pattern is standard in ASP.NET Core services.
// ============================================================

using MiniFlightPlan.API.DTOs;

namespace MiniFlightPlan.API.Services;

public interface IFlightPlanService
{
    // Get all flight plans, optionally filtered by status
    // IEnumerable<T> is lazy; IReadOnlyList<T> is fully materialized — we use the latter
    // because the DB query has already executed by the time we return
    Task<IReadOnlyList<FlightPlanResponse>> GetAllAsync(string? statusFilter = null);

    // Get a single flight plan by ID — returns null if not found
    Task<FlightPlanResponse?> GetByIdAsync(int id);

    // Create a new flight plan — returns the created plan with its new ID
    Task<FlightPlanResponse> CreateAsync(CreateFlightPlanRequest request);

    // Update status — returns null if the flight plan doesn't exist
    Task<FlightPlanResponse?> UpdateStatusAsync(int id, UpdateFlightPlanStatusRequest request);

    // Delete — returns false if not found
    Task<bool> DeleteAsync(int id);
}
