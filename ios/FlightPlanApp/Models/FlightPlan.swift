// ============================================================
// Models/FlightPlan.swift — Swift Data Models
//
// CODABLE PRIMER:
//   Codable = Encodable + Decodable
//   Any Swift type conforming to Codable can be automatically
//   serialized to / from JSON using JSONEncoder / JSONDecoder.
//
//   Property names map directly to JSON keys by default.
//   Use CodingKeys enum to map different names if needed.
//
//   This mirrors the FlightPlanResponse DTO on the backend.
//   Keep these in sync — if the API shape changes, update here too.
// ============================================================

import Foundation

// MARK: - Flight Plan

struct FlightPlan: Codable, Identifiable {
    let id: Int
    let aircraftRegistration: String
    let departureIcao: String
    let departureAirportName: String
    let arrivalIcao: String
    let arrivalAirportName: String
    let estimatedDepartureTime: Date
    let eteMinutes: Int
    let route: String?
    let flightRules: String
    let status: String
    let createdAt: Date
    let filedAt: Date?

    // Computed helper — formats ETE as "2h 35m"
    var formattedEte: String {
        let hours = eteMinutes / 60
        let mins = eteMinutes % 60
        if hours > 0 { return "\(hours)h \(mins)m" }
        return "\(mins)m"
    }

    // Computed helper — returns a color name for the status badge
    var statusColor: String {
        switch status {
        case "Filed":     return "blue"
        case "Active":    return "green"
        case "Closed":    return "gray"
        case "Cancelled": return "red"
        default:          return "orange" // Draft
        }
    }
}

// MARK: - Airport

struct Airport: Codable, Identifiable {
    let id: Int
    let icaoCode: String
    let name: String
    let city: String
    let state: String

    // Display string for picker: "KSEA — Seattle-Tacoma International"
    var displayName: String { "\(icaoCode) — \(name)" }
}

// MARK: - Create Request

// This mirrors CreateFlightPlanRequest on the backend
// Encodable only — we send this to the API, we don't receive it
struct CreateFlightPlanRequest: Encodable {
    let aircraftRegistration: String
    let departureAirportId: Int
    let arrivalAirportId: Int
    let estimatedDepartureTime: Date
    let eteMinutes: Int
    let route: String?
    let flightRules: String
}

// MARK: - Update Status Request

struct UpdateFlightPlanStatusRequest: Encodable {
    let newStatus: String
}

// MARK: - API Error Response

struct APIErrorResponse: Codable {
    let error: String
    let statusCode: Int
}
