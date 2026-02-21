// ============================================================
// Controllers/FlightPlansController.cs — REST API Controller
//
// Controllers handle HTTP concerns ONLY:
//   - Route matching
//   - Parsing request data
//   - Calling the service layer
//   - Returning the right HTTP status code
//
// They should NOT contain business logic — that belongs in services.
//
// [ApiController] attribute enables:
//   - Automatic model validation (returns 400 if DTO annotations fail)
//   - Automatic binding of JSON body to method parameters
//   - Problem Details responses on errors
// ============================================================

using Microsoft.AspNetCore.Mvc;
using MiniFlightPlan.API.DTOs;
using MiniFlightPlan.API.Services;

namespace MiniFlightPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")] // → /api/flightplans
[Produces("application/json")]
public class FlightPlansController : ControllerBase
{
    private readonly IFlightPlanService _service;

    // Constructor injection — DI provides IFlightPlanService automatically
    // The controller never knows (or cares) whether it's the real service or a test mock
    public FlightPlansController(IFlightPlanService service)
    {
        _service = service;
    }

    /// <summary>Get all flight plans, optionally filtered by status</summary>
    /// <param name="status">Optional status filter: Draft, Filed, Active, Closed, Cancelled</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FlightPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        // [FromQuery] binds the 'status' URL parameter: GET /api/flightplans?status=Filed
        var results = await _service.GetAllAsync(status);
        return Ok(results); // 200 OK with JSON body
    }

    /// <summary>Get a specific flight plan by ID</summary>
    [HttpGet("{id:int}")]  // :int = route constraint — only matches integer segments
    [ProducesResponseType(typeof(FlightPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        // The controller decides the HTTP status code based on service result
        return result == null
            ? NotFound(new { message = $"Flight plan {id} not found." }) // 404
            : Ok(result); // 200
    }

    /// <summary>Create a new flight plan</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FlightPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFlightPlanRequest request)
    {
        // [FromBody] binds the JSON request body to the DTO
        // [ApiController] has already validated the DTO annotations before we get here —
        // if validation failed, a 400 was already returned automatically
        var created = await _service.CreateAsync(request);

        // 201 Created — returns the new resource AND a Location header
        // pointing to GET /api/flightplans/{id} — this is REST convention
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update the status of a flight plan</summary>
    [HttpPatch("{id:int}/status")]  // PATCH is correct for partial updates
    [ProducesResponseType(typeof(FlightPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFlightPlanStatusRequest request)
    {
        var result = await _service.UpdateStatusAsync(id, request);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Delete a flight plan (only Draft or Cancelled plans can be deleted)</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted
            ? NoContent() // 204 — success, no body
            : NotFound();
    }
}
