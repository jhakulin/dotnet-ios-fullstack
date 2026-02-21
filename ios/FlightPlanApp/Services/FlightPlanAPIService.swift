// ============================================================
// Services/FlightPlanAPIService.swift — HTTP Client
//
// This layer handles all network communication with the backend.
// Key concepts:
//   - URLSession with async/await (modern Swift concurrency)
//   - JSONDecoder with date decoding strategy
//   - Typed errors for clean error propagation
//   - Generic request helper to avoid repetition
//
// Swift's async/await mirrors .NET's async/await almost exactly —
// same concept, different syntax. This is a good bridge for you.
// ============================================================

import Foundation

// MARK: - API Errors

// Typed errors give callers structured error handling
// instead of stringly-typed error messages
enum APIError: Error, LocalizedError {
    case invalidURL
    case networkError(Error)
    case httpError(statusCode: Int, message: String)
    case decodingError(Error)

    var errorDescription: String? {
        switch self {
        case .invalidURL:
            return "Invalid URL"
        case .networkError(let error):
            return "Network error: \(error.localizedDescription)"
        case .httpError(let code, let message):
            return "Server error \(code): \(message)"
        case .decodingError(let error):
            return "Failed to parse response: \(error.localizedDescription)"
        }
    }
}

// MARK: - API Service

// @MainActor ensures all published property updates happen on the main thread
// (required for SwiftUI — UI updates must be on the main thread)
class FlightPlanAPIService {

    // Base URL of the backend — localhost works in the iOS Simulator
    // On a real device, replace with your Mac's local IP (e.g. http://192.168.1.10:5000)
    private let baseURL = "http://localhost:5000/api"

    // Shared JSONDecoder configured for the API's date format
    // The backend uses ISO 8601 dates (e.g. "2024-01-15T14:30:00Z")
    private let decoder: JSONDecoder = {
        let d = JSONDecoder()
        // .iso8601 strategy parses "2024-01-15T14:30:00Z" automatically
        d.dateDecodingStrategy = .iso8601
        // .convertFromSnakeCase would handle snake_case → camelCase
        // but our backend already returns camelCase so we don't need it
        return d
    }()

    private let encoder: JSONEncoder = {
        let e = JSONEncoder()
        e.dateEncodingStrategy = .iso8601
        return e
    }()

    // MARK: - Flight Plans

    func fetchFlightPlans(status: String? = nil) async throws -> [FlightPlan] {
        var urlString = "\(baseURL)/flightplans"
        if let status = status {
            urlString += "?status=\(status)"
        }
        return try await get(urlString)
    }

    func fetchFlightPlan(id: Int) async throws -> FlightPlan {
        return try await get("\(baseURL)/flightplans/\(id)")
    }

    func createFlightPlan(_ request: CreateFlightPlanRequest) async throws -> FlightPlan {
        return try await post("\(baseURL)/flightplans", body: request)
    }

    func updateFlightPlanStatus(id: Int, newStatus: String) async throws -> FlightPlan {
        let request = UpdateFlightPlanStatusRequest(newStatus: newStatus)
        return try await patch("\(baseURL)/flightplans/\(id)/status", body: request)
    }

    func deleteFlightPlan(id: Int) async throws {
        try await delete("\(baseURL)/flightplans/\(id)")
    }

    // MARK: - Airports

    func fetchAirports() async throws -> [Airport] {
        return try await get("\(baseURL)/airports")
    }

    // MARK: - Generic HTTP Helpers
    // These reduce boilerplate — every endpoint uses these instead of
    // duplicating URLSession setup, error handling, and decoding

    private func get<T: Decodable>(_ urlString: String) async throws -> T {
        guard let url = URL(string: urlString) else { throw APIError.invalidURL }

        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.setValue("application/json", forHTTPHeaderField: "Accept")

        return try await execute(request)
    }

    private func post<T: Decodable, B: Encodable>(_ urlString: String, body: B) async throws -> T {
        guard let url = URL(string: urlString) else { throw APIError.invalidURL }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        request.httpBody = try encoder.encode(body)

        return try await execute(request)
    }

    private func patch<T: Decodable, B: Encodable>(_ urlString: String, body: B) async throws -> T {
        guard let url = URL(string: urlString) else { throw APIError.invalidURL }

        var request = URLRequest(url: url)
        request.httpMethod = "PATCH"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        request.httpBody = try encoder.encode(body)

        return try await execute(request)
    }

    private func delete(_ urlString: String) async throws {
        guard let url = URL(string: urlString) else { throw APIError.invalidURL }

        var request = URLRequest(url: url)
        request.httpMethod = "DELETE"

        // For DELETE we don't decode a response body (204 No Content)
        let (_, response) = try await URLSession.shared.data(for: request)
        guard let httpResponse = response as? HTTPURLResponse else { return }
        if httpResponse.statusCode == 404 {
            throw APIError.httpError(statusCode: 404, message: "Not found")
        }
    }

    // Core execution — makes the request, checks HTTP status, decodes JSON
    private func execute<T: Decodable>(_ request: URLRequest) async throws -> T {
        let data: Data
        let response: URLResponse

        do {
            // async/await URLSession — no callbacks, clean linear code
            (data, response) = try await URLSession.shared.data(for: request)
        } catch {
            throw APIError.networkError(error)
        }

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError(URLError(.badServerResponse))
        }

        // Check for non-2xx HTTP status codes
        if !(200...299).contains(httpResponse.statusCode) {
            // Try to decode the error message from the backend's error response body
            let errorMessage: String
            if let apiError = try? decoder.decode(APIErrorResponse.self, from: data) {
                errorMessage = apiError.error
            } else {
                errorMessage = HTTPURLResponse.localizedString(forStatusCode: httpResponse.statusCode)
            }
            throw APIError.httpError(statusCode: httpResponse.statusCode, message: errorMessage)
        }

        // Decode the JSON response body into the expected type
        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            throw APIError.decodingError(error)
        }
    }
}
