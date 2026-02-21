// ============================================================
// Views/ContentView.swift — App Root View
// ============================================================

import SwiftUI

struct ContentView: View {
    // @StateObject creates and owns the ViewModel for the lifetime of this view
    // Use @StateObject at the root, @ObservedObject when passing down to children
    @StateObject private var viewModel = FlightPlanViewModel()

    var body: some View {
        NavigationStack {
            FlightPlanListView()
                .environmentObject(viewModel) // passes viewModel down to all children
        }
        .task {
            // .task runs async code when the view appears, and cancels it when dismissed
            // This is the SwiftUI way to kick off async work on view appearance
            await viewModel.loadFlightPlans()
            await viewModel.loadAirports()
        }
    }
}
