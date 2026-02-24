// ============================================================
// Integration Tests for FlightPlansController
//
// These tests exercise the full ASP.NET Core pipeline:
//   HTTP request → middleware → routing → controller → service → DB
//
// Key concepts demonstrated:
//   - WebApplicationFactory spins up the real app in-process
//   - HttpClient talks to it just like a real client would
//   - Tests verify HTTP status codes, headers, and response bodies
//   - IClassFixture shares one factory instance across all tests
//     in this class (faster than creating a new app per test)
// ============================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MiniFlightPlan.API.DTOs;
using MiniFlightPlan.API.Models;

namespace MiniFlightPlan.Tests.Integration;

// IClassFixture tells xUnit to create one factory for all tests in this class
public class FlightPlansControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FlightPlansControllerTests(CustomWebApplicationFactory factory)
    {
        // CreateClient() builds an HttpClient pre-wired to the in-process test server
        _client = factory.CreateClient();
    }

    // ── GET /api/flightplans ──────────────────────────────────

    [Fact]
    public async Task GetFlightPlans_ReturnsOkWithSeededData()
    {
        // Act — just like a real client hitting the endpoint
        var response = await _client.GetAsync("/api/flightplans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plans = await response.Content.ReadFromJsonAsync<List<FlightPlanResponse>>();
        plans.Should().NotBeNull();
        plans!.Count.Should().BeGreaterThan(0, "SeedData seeds 2 flight plans on startup");
    }

    [Fact]
    public async Task GetFlightPlans_WithStatusFilter_ReturnsOnlyMatchingPlans()
    {
        // Act
        var response = await _client.GetAsync("/api/flightplans?status=Filed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plans = await response.Content.ReadFromJsonAsync<List<FlightPlanResponse>>();
        plans.Should().NotBeNull();
        plans!.Should().AllSatisfy(p => p.Status.Should().Be("Filed"));
    }

    // ── POST /api/flightplans ─────────────────────────────────

    [Fact]
    public async Task CreateFlightPlan_ValidRequest_Returns201WithLocationHeader()
    {
        // Arrange — get real airport IDs from the seeded data
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();
        airports.Should().NotBeNull().And.HaveCountGreaterThan(1);

        var request = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N99999",
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[1].Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(2),
            EteMinutes = 90,
            Route = "KSEA SEA J80 KPHX",
            FlightRules = FlightRules.IFR
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/flightplans", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull("201 Created must include a Location header");

        var created = await response.Content.ReadFromJsonAsync<FlightPlanResponse>();
        created!.AircraftRegistration.Should().Be("N99999");
        created.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateFlightPlan_SameAirports_Returns400()
    {
        // Arrange
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();

        var request = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N11111",
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[0].Id,  // same — invalid
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 60,
            FlightRules = FlightRules.VFR
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/flightplans", request);

        // Assert — ErrorHandlingMiddleware maps InvalidOperationException → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PATCH /api/flightplans/{id}/status ───────────────────

    [Fact]
    public async Task UpdateStatus_DraftToFiled_ReturnsUpdatedPlan()
    {
        // Arrange — create a Draft plan first
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();

        var createRequest = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N55555",
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[1].Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(3),
            EteMinutes = 75,
            FlightRules = FlightRules.IFR
        };
        var createResponse = await _client.PostAsJsonAsync("/api/flightplans", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<FlightPlanResponse>();

        var statusRequest = new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Filed };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/flightplans/{created!.Id}/status", statusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<FlightPlanResponse>();
        updated!.Status.Should().Be("Filed");
        updated.FiledAt.Should().NotBeNull();
    }

    // ── DELETE /api/flightplans/{id} ──────────────────────────

    [Fact]
    public async Task DeleteFlightPlan_DraftPlan_Returns204()
    {
        // Arrange — create a Draft plan to delete
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();

        var createRequest = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N44444",
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[1].Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(4),
            EteMinutes = 50,
            FlightRules = FlightRules.VFR
        };
        var createResponse = await _client.PostAsJsonAsync("/api/flightplans", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<FlightPlanResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/flightplans/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetFlightPlan_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/flightplans/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH — error cases ───────────────────────────────────

    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns400()
    {
        // Arrange — create a Draft plan, file it, activate it, close it (terminal)
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();

        var createReq = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N10101",
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[1].Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(5),
            EteMinutes = 60,
            FlightRules = FlightRules.IFR
        };
        var createResp = await _client.PostAsJsonAsync("/api/flightplans", createReq);
        var plan = await createResp.Content.ReadFromJsonAsync<FlightPlanResponse>();

        // Advance to Closed (terminal)
        await _client.PatchAsJsonAsync($"/api/flightplans/{plan!.Id}/status",
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Filed });
        await _client.PatchAsJsonAsync($"/api/flightplans/{plan.Id}/status",
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Active });
        await _client.PatchAsJsonAsync($"/api/flightplans/{plan.Id}/status",
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Closed });

        // Act — Closed → Active is an invalid transition
        var response = await _client.PatchAsJsonAsync($"/api/flightplans/{plan.Id}/status",
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Active });

        // Assert — ErrorHandlingMiddleware maps InvalidOperationException → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DELETE — error cases ──────────────────────────────────

    [Fact]
    public async Task DeleteFlightPlan_ActivePlan_Returns400()
    {
        // Arrange — create a plan and advance it to Active
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();

        var createReq = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N20202",
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[1].Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(6),
            EteMinutes = 45,
            FlightRules = FlightRules.VFR
        };
        var createResp = await _client.PostAsJsonAsync("/api/flightplans", createReq);
        var plan = await createResp.Content.ReadFromJsonAsync<FlightPlanResponse>();

        await _client.PatchAsJsonAsync($"/api/flightplans/{plan!.Id}/status",
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Filed });
        await _client.PatchAsJsonAsync($"/api/flightplans/{plan.Id}/status",
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Active });

        // Act — Active plans cannot be deleted
        var response = await _client.DeleteAsync($"/api/flightplans/{plan.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteFlightPlan_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/flightplans/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST — DTO validation ─────────────────────────────────

    [Fact]
    public async Task CreateFlightPlan_AircraftRegistrationTooShort_Returns400()
    {
        // Arrange — registration "AB" is only 2 chars; minimum is 3
        var airportsResponse = await _client.GetAsync("/api/airports");
        var airports = await airportsResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();

        var request = new CreateFlightPlanRequest
        {
            AircraftRegistration = "AB",   // violates [StringLength(10, MinimumLength = 3)]
            DepartureAirportId = airports![0].Id,
            ArrivalAirportId = airports[1].Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 60,
            FlightRules = FlightRules.VFR
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/flightplans", request);

        // Assert — [ApiController] returns 400 automatically for invalid model state
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
