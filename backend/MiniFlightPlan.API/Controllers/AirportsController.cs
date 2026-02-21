// ============================================================
// Controllers/AirportsController.cs
//
// A simpler controller that also demonstrates IMemoryCache —
// airports rarely change, so caching the list makes sense.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniFlightPlan.API.Data;
using MiniFlightPlan.API.DTOs;

namespace MiniFlightPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AirportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    // Injecting both DbContext and IMemoryCache from the DI container
    // Both were registered in Program.cs
    public AirportsController(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private const string AirportsCacheKey = "airports_all";

    /// <summary>Get all airports</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AirportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        // IMemoryCache pattern: try to get from cache, fall back to DB if miss
        // GetOrCreateAsync handles the "check + set" atomically
        var airports = await _cache.GetOrCreateAsync(AirportsCacheKey, async entry =>
        {
            // Cache for 10 minutes — airports change very rarely
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            // This only runs on cache miss — the DB query
            return await _db.Airports
                .OrderBy(a => a.IcaoCode)
                .Select(a => new AirportResponse
                {
                    Id = a.Id,
                    IcaoCode = a.IcaoCode,
                    Name = a.Name,
                    City = a.City,
                    State = a.State
                })
                .ToListAsync();
        });

        return Ok(airports);
    }

    /// <summary>Get a single airport by ICAO code</summary>
    [HttpGet("{icaoCode}")]
    [ProducesResponseType(typeof(AirportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIcao(string icaoCode)
    {
        var airport = await _db.Airports
            .FirstOrDefaultAsync(a => a.IcaoCode == icaoCode.ToUpperInvariant());

        if (airport == null)
            return NotFound(new { message = $"Airport '{icaoCode}' not found." });

        return Ok(new AirportResponse
        {
            Id = airport.Id,
            IcaoCode = airport.IcaoCode,
            Name = airport.Name,
            City = airport.City,
            State = airport.State
        });
    }
}
