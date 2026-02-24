// ============================================================
// Integration Tests for AirportsController
//
// Exercises both endpoints:
//   GET /api/airports          — returns all airports (cached)
//   GET /api/airports/{icao}   — returns one airport by ICAO code
//
// Uses the same CustomWebApplicationFactory as the flight plan
// tests, but via a separate IClassFixture so each test class
// gets its own isolated in-memory database instance.
// ============================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MiniFlightPlan.API.DTOs;

namespace MiniFlightPlan.Tests.Integration;

public class AirportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AirportsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/airports ─────────────────────────────────────

    [Fact]
    public async Task GetAirports_ReturnsOkWithSeededAirports()
    {
        // Act
        var response = await _client.GetAsync("/api/airports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var airports = await response.Content.ReadFromJsonAsync<List<AirportResponse>>();
        airports.Should().NotBeNull();
        airports!.Count.Should().BeGreaterThan(0, "SeedData seeds airports on startup");
    }

    [Fact]
    public async Task GetAirports_ResponseContainsExpectedFields()
    {
        // Act
        var response = await _client.GetAsync("/api/airports");
        var airports = await response.Content.ReadFromJsonAsync<List<AirportResponse>>();

        // Assert — every airport DTO should have all required fields populated
        airports.Should().AllSatisfy(a =>
        {
            a.Id.Should().BeGreaterThan(0);
            a.IcaoCode.Should().NotBeNullOrWhiteSpace();
            a.Name.Should().NotBeNullOrWhiteSpace();
            a.City.Should().NotBeNullOrWhiteSpace();
            a.State.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── GET /api/airports/{icaoCode} ──────────────────────────

    [Fact]
    public async Task GetAirportByIcao_ExistingCode_ReturnsOkWithAirport()
    {
        // Arrange — get a known ICAO code from the seeded list
        var listResponse = await _client.GetAsync("/api/airports");
        var airports = await listResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();
        var firstIcao = airports![0].IcaoCode;

        // Act
        var response = await _client.GetAsync($"/api/airports/{firstIcao}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var airport = await response.Content.ReadFromJsonAsync<AirportResponse>();
        airport.Should().NotBeNull();
        airport!.IcaoCode.Should().Be(firstIcao);
    }

    [Fact]
    public async Task GetAirportByIcao_LookupIsCaseInsensitive()
    {
        // Arrange — ICAO codes are stored upper-case; query with lower-case
        var listResponse = await _client.GetAsync("/api/airports");
        var airports = await listResponse.Content.ReadFromJsonAsync<List<AirportResponse>>();
        var lowerCaseIcao = airports![0].IcaoCode.ToLower();

        // Act
        var response = await _client.GetAsync($"/api/airports/{lowerCaseIcao}");

        // Assert — controller calls .ToUpperInvariant() before querying
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAirportByIcao_NonExistentCode_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/airports/ZZZZ");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
