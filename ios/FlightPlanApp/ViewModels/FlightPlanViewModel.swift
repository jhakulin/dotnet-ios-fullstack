// ============================================================
// ViewModels/FlightPlanViewModel.swift — The ViewModel
//
// MVVM PRIMER:
//   ViewModel sits between the View and the data layer (API service).
//   It exposes @Published properties that SwiftUI views observe.
//   When a @Published property changes, any SwiftUI view referencing
//   it automatically re-renders.
//
//   This is conceptually similar to INotifyPropertyChanged in .NET XAML,
//   or React state if you're familiar with that.
//
//   ObservableObject = the protocol that makes @Published work
//   @Published = marks a property as observable by SwiftUI views
//   @MainActor = ensures UI updates happen on the main thread
// ============================================================

import Foundation
import Combine

@MainActor
class FlightPlanViewModel: ObservableObject {

    // @Published — any SwiftUI view that reads these will re-render when they change
    @Published var flightPlans: [FlightPlan] = []
    @Published var airports: [Airport] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var selectedStatusFilter: String? = nil

    private let apiService = FlightPlanAPIService()

    // MARK: - Flight Plans

    func loadFlightPlans() async {
        isLoading = true
        errorMessage = nil

        do {
            // async/await — clean, readable, no callback pyramid
            flightPlans = try await apiService.fetchFlightPlans(status: selectedStatusFilter)
        } catch {
            // Surface the typed APIError message to the UI
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func createFlightPlan(
        aircraftReg: String,
        departureAirportId: Int,
        arrivalAirportId: Int,
        departureTime: Date,
        eteMinutes: Int,
        route: String?,
        flightRules: String
    ) async -> Bool {
        isLoading = true
        errorMessage = nil

        let request = CreateFlightPlanRequest(
            aircraftRegistration: aircraftReg,
            departureAirportId: departureAirportId,
            arrivalAirportId: arrivalAirportId,
            estimatedDepartureTime: departureTime,
            eteMinutes: eteMinutes,
            route: route?.isEmpty == true ? nil : route,
            flightRules: flightRules
        )

        do {
            let created = try await apiService.createFlightPlan(request)
            // Optimistic insert — add to local list without reloading everything
            flightPlans.append(created)
            // Sort by departure time to keep list ordered
            flightPlans.sort { $0.estimatedDepartureTime < $1.estimatedDepartureTime }
            isLoading = false
            return true
        } catch {
            errorMessage = error.localizedDescription
            isLoading = false
            return false
        }
    }

    func updateStatus(id: Int, newStatus: String) async {
        do {
            let updated = try await apiService.updateFlightPlanStatus(id: id, newStatus: newStatus)
            // Update the matching item in the local list
            if let index = flightPlans.firstIndex(where: { $0.id == id }) {
                flightPlans[index] = updated
            }
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func deleteFlightPlan(id: Int) async {
        do {
            try await apiService.deleteFlightPlan(id: id)
            flightPlans.removeAll { $0.id == id }
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // MARK: - Airports

    func loadAirports() async {
        do {
            airports = try await apiService.fetchAirports()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // MARK: - Helpers

    var statusFilters: [String?] {
        [nil, "Draft", "Filed", "Active", "Closed", "Cancelled"]
    }

    func filterLabel(_ filter: String?) -> String {
        filter ?? "All"
    }

    func nextValidStatuses(for flightPlan: FlightPlan) -> [String] {
        switch flightPlan.status {
        case "Draft":     return ["Filed", "Cancelled"]
        case "Filed":     return ["Active", "Cancelled"]
        case "Active":    return ["Closed", "Cancelled"]
        default:          return []
        }
    }
}
