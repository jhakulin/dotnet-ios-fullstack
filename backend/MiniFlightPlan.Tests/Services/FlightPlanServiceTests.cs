// ============================================================
// Unit Tests for FlightPlanService
//
// Uses EF Core InMemory database — no real SQLite, no HTTP.
// Each test gets its own uniquely-named database so they
// are fully isolated and can run in parallel.
//
// Demonstrates:
//   - Arrange / Act / Assert pattern
//   - In-memory DbContext setup
//   - Testing business logic in isolation from HTTP
//   - FluentAssertions for expressive assertions
// ============================================================

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MiniFlightPlan.API.Data;
using MiniFlightPlan.API.DTOs;
using MiniFlightPlan.API.Models;
using MiniFlightPlan.API.Services;

namespace MiniFlightPlan.Tests.Services;

public class FlightPlanServiceTests
{
    // ── HELPERS ──────────────────────────────────────────────

    /// <summary>
    /// Creates a fresh in-memory DbContext per test.
    /// Each test passes its method name as the DB name so
    /// they never share state — same idea as database isolation
    /// in integration testing.
    /// </summary>
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Seeds two airports and returns them with the context.
    /// Most tests need airports first before they can create flight plans.
    /// </summary>
    private static async Task<(AppDbContext Context, Airport Departure, Airport Arrival)>
        SeedAirportsAsync(string dbName)
    {
        var context = CreateContext(dbName);
        var departure = new Airport
        {
            IcaoCode = "KSEA",
            Name = "Seattle-Tacoma International",
            City = "Seattle",
            State = "WA"
        };
        var arrival = new Airport
        {
            IcaoCode = "KPHX",
            Name = "Phoenix Sky Harbor International",
            City = "Phoenix",
            State = "AZ"
        };
        context.Airports.AddRange(departure, arrival);
        await context.SaveChangesAsync();
        return (context, departure, arrival);
    }

    // ── GET TESTS ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllPlans()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(GetAllAsync_NoFilter_ReturnsAllPlans));
        var service = new FlightPlanService(context);

        context.FlightPlans.AddRange(
            new FlightPlan
            {
                AircraftRegistration = "N11111",
                DepartureAirportId = dep.Id, ArrivalAirportId = arr.Id,
                EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
                EteMinutes = 60, FlightRules = FlightRules.VFR,
                Status = FlightStatus.Draft, CreatedAt = DateTime.UtcNow
            },
            new FlightPlan
            {
                AircraftRegistration = "N22222",
                DepartureAirportId = dep.Id, ArrivalAirportId = arr.Id,
                EstimatedDepartureTime = DateTime.UtcNow.AddDays(2),
                EteMinutes = 90, FlightRules = FlightRules.IFR,
                Status = FlightStatus.Filed, CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetAllAsync();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsOnlyMatchingPlans()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(GetAllAsync_WithStatusFilter_ReturnsOnlyMatchingPlans));
        var service = new FlightPlanService(context);

        context.FlightPlans.AddRange(
            new FlightPlan
            {
                AircraftRegistration = "N11111",
                DepartureAirportId = dep.Id, ArrivalAirportId = arr.Id,
                EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
                EteMinutes = 60, FlightRules = FlightRules.VFR,
                Status = FlightStatus.Draft, CreatedAt = DateTime.UtcNow
            },
            new FlightPlan
            {
                AircraftRegistration = "N22222",
                DepartureAirportId = dep.Id, ArrivalAirportId = arr.Id,
                EstimatedDepartureTime = DateTime.UtcNow.AddDays(2),
                EteMinutes = 90, FlightRules = FlightRules.IFR,
                Status = FlightStatus.Filed, CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetAllAsync("Draft");

        // Assert
        results.Should().HaveCount(1);
        results[0].Status.Should().Be("Draft");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMappedResponse()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(GetByIdAsync_ExistingId_ReturnsMappedResponse));
        var service = new FlightPlanService(context);

        var plan = new FlightPlan
        {
            AircraftRegistration = "N33333",
            DepartureAirportId = dep.Id, ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 75, FlightRules = FlightRules.IFR,
            Status = FlightStatus.Draft, CreatedAt = DateTime.UtcNow
        };
        context.FlightPlans.Add(plan);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(plan.Id);

        // Assert
        result.Should().NotBeNull();
        result!.AircraftRegistration.Should().Be("N33333");
        result.DepartureIcao.Should().Be("KSEA");
        result.ArrivalIcao.Should().Be("KPHX");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var context = CreateContext(nameof(GetByIdAsync_NonExistentId_ReturnsNull));
        var service = new FlightPlanService(context);

        // Act
        var result = await service.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    // ── CREATE TESTS ─────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewFlightPlan_StatusIsDraft()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(CreateAsync_NewFlightPlan_StatusIsDraft));
        var service = new FlightPlanService(context);

        var request = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N12345",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 120,
            FlightRules = FlightRules.IFR
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Status.Should().Be("Draft");
        result.AircraftRegistration.Should().Be("N12345");
        result.DepartureIcao.Should().Be("KSEA");
        result.ArrivalIcao.Should().Be("KPHX");
    }

    [Fact]
    public async Task CreateAsync_SameAirports_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, dep, _) = await SeedAirportsAsync(nameof(CreateAsync_SameAirports_ThrowsInvalidOperationException));
        var service = new FlightPlanService(context);

        var request = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N12345",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = dep.Id,      // same airport — invalid
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 60,
            FlightRules = FlightRules.VFR
        };

        // Act & Assert
        await service.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different*");
    }

    [Fact]
    public async Task CreateAsync_PastDepartureTime_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(CreateAsync_PastDepartureTime_ThrowsInvalidOperationException));
        var service = new FlightPlanService(context);

        var request = new CreateFlightPlanRequest
        {
            AircraftRegistration = "N12345",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(-1),   // in the past — invalid
            EteMinutes = 120,
            FlightRules = FlightRules.IFR
        };

        // Act & Assert
        await service.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*future*");
    }

    // ── STATUS TRANSITION TESTS ───────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_DraftToFiled_SetsFiledAt()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(UpdateStatusAsync_DraftToFiled_SetsFiledAt));
        var service = new FlightPlanService(context);

        var plan = new FlightPlan
        {
            AircraftRegistration = "N88888",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 90,
            FlightRules = FlightRules.IFR,
            Status = FlightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        context.FlightPlans.Add(plan);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateStatusAsync(
            plan.Id,
            new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Filed });

        // Assert
        result!.Status.Should().Be("Filed");
        result.FiledAt.Should().NotBeNull("FiledAt must be set when transitioning to Filed");
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange — put plan into Closed (terminal state)
        var (context, dep, arr) = await SeedAirportsAsync(nameof(UpdateStatusAsync_InvalidTransition_ThrowsInvalidOperationException));
        var service = new FlightPlanService(context);

        var plan = new FlightPlan
        {
            AircraftRegistration = "N99999",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 60,
            FlightRules = FlightRules.IFR,
            Status = FlightStatus.Closed,   // terminal state
            CreatedAt = DateTime.UtcNow
        };
        context.FlightPlans.Add(plan);
        await context.SaveChangesAsync();

        // Act & Assert — Closed → Active is not a valid transition
        await service.Invoking(s => s.UpdateStatusAsync(
                plan.Id,
                new UpdateFlightPlanStatusRequest { NewStatus = FlightStatus.Active }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid status transition*");
    }

    // ── DELETE TESTS ─────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ActivePlan_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(DeleteAsync_ActivePlan_ThrowsInvalidOperationException));
        var service = new FlightPlanService(context);

        var plan = new FlightPlan
        {
            AircraftRegistration = "N77777",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 60,
            FlightRules = FlightRules.VFR,
            Status = FlightStatus.Active,   // active plans cannot be deleted
            CreatedAt = DateTime.UtcNow
        };
        context.FlightPlans.Add(plan);
        await context.SaveChangesAsync();

        // Act & Assert
        await service.Invoking(s => s.DeleteAsync(plan.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot delete*");
    }

    [Fact]
    public async Task DeleteAsync_DraftPlan_ReturnsTrue()
    {
        // Arrange
        var (context, dep, arr) = await SeedAirportsAsync(nameof(DeleteAsync_DraftPlan_ReturnsTrue));
        var service = new FlightPlanService(context);

        var plan = new FlightPlan
        {
            AircraftRegistration = "N66666",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            EstimatedDepartureTime = DateTime.UtcNow.AddDays(1),
            EteMinutes = 45,
            FlightRules = FlightRules.VFR,
            Status = FlightStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        context.FlightPlans.Add(plan);
        await context.SaveChangesAsync();

        // Act
        var deleted = await service.DeleteAsync(plan.Id);

        // Assert
        deleted.Should().BeTrue();
        context.FlightPlans.Should().BeEmpty();
    }
}
