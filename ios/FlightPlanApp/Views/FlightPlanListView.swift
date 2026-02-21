// ============================================================
// Views/FlightPlanListView.swift — Main List Screen
// ============================================================

import SwiftUI

struct FlightPlanListView: View {
    // @EnvironmentObject receives the ViewModel injected by ContentView
    // Any view in the hierarchy can access it without explicit passing
    @EnvironmentObject private var viewModel: FlightPlanViewModel
    @State private var showingCreateSheet = false

    var body: some View {
        Group {
            if viewModel.isLoading && viewModel.flightPlans.isEmpty {
                ProgressView("Loading flight plans...")
            } else if viewModel.flightPlans.isEmpty {
                emptyState
            } else {
                flightPlanList
            }
        }
        .navigationTitle("Flight Plans")
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button {
                    showingCreateSheet = true
                } label: {
                    Image(systemName: "plus")
                }
            }
            ToolbarItem(placement: .navigationBarLeading) {
                filterMenu
            }
        }
        .sheet(isPresented: $showingCreateSheet) {
            CreateFlightPlanView()
                .environmentObject(viewModel)
        }
        .refreshable {
            // Pull-to-refresh — calls loadFlightPlans on the viewModel
            await viewModel.loadFlightPlans()
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") { viewModel.errorMessage = nil }
        } message: {
            Text(viewModel.errorMessage ?? "")
        }
    }

    private var flightPlanList: some View {
        List {
            ForEach(viewModel.flightPlans) { flightPlan in
                NavigationLink(destination: FlightPlanDetailView(flightPlan: flightPlan)) {
                    FlightPlanRowView(flightPlan: flightPlan)
                }
            }
            .onDelete { indexSet in
                Task {
                    for index in indexSet {
                        await viewModel.deleteFlightPlan(id: viewModel.flightPlans[index].id)
                    }
                }
            }
        }
    }

    private var emptyState: some View {
        VStack(spacing: 16) {
            Image(systemName: "airplane")
                .font(.system(size: 60))
                .foregroundColor(.secondary)
            Text("No flight plans")
                .font(.title2)
                .foregroundColor(.secondary)
            Button("Create your first flight plan") {
                showingCreateSheet = true
            }
            .buttonStyle(.borderedProminent)
        }
    }

    private var filterMenu: some View {
        Menu {
            ForEach(viewModel.statusFilters, id: \.self) { filter in
                Button {
                    viewModel.selectedStatusFilter = filter
                    Task { await viewModel.loadFlightPlans() }
                } label: {
                    HStack {
                        Text(viewModel.filterLabel(filter))
                        if viewModel.selectedStatusFilter == filter {
                            Image(systemName: "checkmark")
                        }
                    }
                }
            }
        } label: {
            Label(
                viewModel.filterLabel(viewModel.selectedStatusFilter),
                systemImage: "line.3.horizontal.decrease.circle"
            )
        }
    }
}

// MARK: - Row View

struct FlightPlanRowView: View {
    let flightPlan: FlightPlan

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                // Route header: KSEA → KPHX
                Text("\(flightPlan.departureIcao) → \(flightPlan.arrivalIcao)")
                    .font(.headline)
                Spacer()
                StatusBadge(status: flightPlan.status)
            }

            HStack {
                Label(flightPlan.aircraftRegistration, systemImage: "airplane")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                Spacer()
                Text(flightPlan.estimatedDepartureTime, style: .time)
                    .font(.subheadline)
                    .foregroundColor(.secondary)
            }

            if let route = flightPlan.route {
                Text(route)
                    .font(.caption)
                    .foregroundColor(.secondary)
                    .lineLimit(1)
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - Status Badge

struct StatusBadge: View {
    let status: String

    var color: Color {
        switch status {
        case "Filed":     return .blue
        case "Active":    return .green
        case "Closed":    return .gray
        case "Cancelled": return .red
        default:          return .orange // Draft
        }
    }

    var body: some View {
        Text(status)
            .font(.caption)
            .fontWeight(.semibold)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(color.opacity(0.15))
            .foregroundColor(color)
            .clipShape(Capsule())
    }
}
