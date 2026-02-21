// ============================================================
// Views/FlightPlanDetailView.swift — Detail Screen
// ============================================================

import SwiftUI

struct FlightPlanDetailView: View {
    @EnvironmentObject private var viewModel: FlightPlanViewModel
    let flightPlan: FlightPlan

    // Find the live version from the viewModel (so status updates reflect immediately)
    private var livePlan: FlightPlan {
        viewModel.flightPlans.first(where: { $0.id == flightPlan.id }) ?? flightPlan
    }

    var body: some View {
        List {
            Section("Route") {
                DetailRow(label: "Departure", value: "\(livePlan.departureIcao) — \(livePlan.departureAirportName)")
                DetailRow(label: "Arrival",   value: "\(livePlan.arrivalIcao) — \(livePlan.arrivalAirportName)")
                if let route = livePlan.route {
                    DetailRow(label: "Route", value: route)
                }
            }

            Section("Flight Details") {
                DetailRow(label: "Aircraft",     value: livePlan.aircraftRegistration)
                DetailRow(label: "Flight Rules", value: livePlan.flightRules)
                DetailRow(label: "ETE",          value: livePlan.formattedEte)
                DetailRow(label: "Departure",    value: livePlan.estimatedDepartureTime.formatted(date: .abbreviated, time: .shortened))
            }

            Section("Status") {
                HStack {
                    Text("Current Status")
                    Spacer()
                    StatusBadge(status: livePlan.status)
                }

                // Show status transition buttons based on current status
                let nextStatuses = viewModel.nextValidStatuses(for: livePlan)
                if !nextStatuses.isEmpty {
                    ForEach(nextStatuses, id: \.self) { nextStatus in
                        Button {
                            Task {
                                await viewModel.updateStatus(id: livePlan.id, newStatus: nextStatus)
                            }
                        } label: {
                            Label("Mark as \(nextStatus)", systemImage: statusIcon(nextStatus))
                        }
                        .foregroundColor(statusActionColor(nextStatus))
                    }
                }
            }

            if let filedAt = livePlan.filedAt {
                Section("Timeline") {
                    DetailRow(label: "Filed At", value: filedAt.formatted(date: .abbreviated, time: .shortened))
                    DetailRow(label: "Created",  value: livePlan.createdAt.formatted(date: .abbreviated, time: .shortened))
                }
            }
        }
        .navigationTitle("\(livePlan.departureIcao) → \(livePlan.arrivalIcao)")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func statusIcon(_ status: String) -> String {
        switch status {
        case "Filed":     return "paperplane.fill"
        case "Active":    return "airplane"
        case "Closed":    return "checkmark.circle.fill"
        case "Cancelled": return "xmark.circle.fill"
        default:          return "circle"
        }
    }

    private func statusActionColor(_ status: String) -> Color {
        status == "Cancelled" ? .red : .blue
    }
}

struct DetailRow: View {
    let label: String
    let value: String

    var body: some View {
        HStack {
            Text(label)
                .foregroundColor(.secondary)
            Spacer()
            Text(value)
                .multilineTextAlignment(.trailing)
        }
    }
}

// ============================================================
// Views/CreateFlightPlanView.swift — Create Screen
// ============================================================

struct CreateFlightPlanView: View {
    @EnvironmentObject private var viewModel: FlightPlanViewModel
    @Environment(\.dismiss) private var dismiss

    // Form state — @State is local view state, not shared
    @State private var aircraftReg = ""
    @State private var selectedDepartureId: Int?
    @State private var selectedArrivalId: Int?
    @State private var departureTime = Date().addingTimeInterval(3600)
    @State private var eteHours = 1
    @State private var eteMinutes = 30
    @State private var route = ""
    @State private var selectedFlightRules = "IFR"
    @State private var isSubmitting = false
    @State private var validationError: String?

    private let flightRulesOptions = ["VFR", "IFR"]

    var body: some View {
        NavigationStack {
            Form {
                Section("Aircraft") {
                    TextField("Registration (e.g. N12345)", text: $aircraftReg)
                        .autocapitalization(.allCharacters)

                    Picker("Flight Rules", selection: $selectedFlightRules) {
                        ForEach(flightRulesOptions, id: \.self) { rules in
                            Text(rules).tag(rules)
                        }
                    }
                    .pickerStyle(.segmented)
                }

                Section("Route") {
                    // Airport pickers — populated from viewModel.airports (loaded from API)
                    Picker("Departure Airport", selection: $selectedDepartureId) {
                        Text("Select...").tag(Optional<Int>.none)
                        ForEach(viewModel.airports) { airport in
                            Text(airport.displayName).tag(Optional(airport.id))
                        }
                    }

                    Picker("Arrival Airport", selection: $selectedArrivalId) {
                        Text("Select...").tag(Optional<Int>.none)
                        ForEach(viewModel.airports) { airport in
                            Text(airport.displayName).tag(Optional(airport.id))
                        }
                    }

                    TextField("Route (optional, e.g. SEA V23 PDX)", text: $route)
                }

                Section("Timing") {
                    DatePicker(
                        "Estimated Departure",
                        selection: $departureTime,
                        in: Date()..., // only future dates
                        displayedComponents: [.date, .hourAndMinute]
                    )

                    Stepper("Hours: \(eteHours)", value: $eteHours, in: 0...24)
                    Stepper("Minutes: \(eteMinutes)", value: $eteMinutes, in: 0...59, step: 5)
                    Text("Total ETE: \(eteHours)h \(eteMinutes)m")
                        .foregroundColor(.secondary)
                        .font(.caption)
                }

                if let error = validationError {
                    Section {
                        Text(error)
                            .foregroundColor(.red)
                            .font(.callout)
                    }
                }
            }
            .navigationTitle("New Flight Plan")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("File") {
                        Task { await submitFlightPlan() }
                    }
                    .disabled(isSubmitting)
                    .overlay {
                        if isSubmitting { ProgressView().scaleEffect(0.7) }
                    }
                }
            }
        }
    }

    private func submitFlightPlan() async {
        // Client-side validation before hitting the API
        guard !aircraftReg.trimmingCharacters(in: .whitespaces).isEmpty else {
            validationError = "Aircraft registration is required."
            return
        }
        guard let departureId = selectedDepartureId else {
            validationError = "Please select a departure airport."
            return
        }
        guard let arrivalId = selectedArrivalId else {
            validationError = "Please select an arrival airport."
            return
        }
        guard departureId != arrivalId else {
            validationError = "Departure and arrival airports must be different."
            return
        }

        validationError = nil
        isSubmitting = true

        let totalEteMinutes = (eteHours * 60) + eteMinutes
        let success = await viewModel.createFlightPlan(
            aircraftReg: aircraftReg.uppercased(),
            departureAirportId: departureId,
            arrivalAirportId: arrivalId,
            departureTime: departureTime,
            eteMinutes: max(totalEteMinutes, 1),
            route: route.isEmpty ? nil : route,
            flightRules: selectedFlightRules
        )

        isSubmitting = false
        if success { dismiss() }
        else { validationError = viewModel.errorMessage }
    }
}
