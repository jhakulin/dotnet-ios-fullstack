# MiniFlightPlan — Learning Project

A mini FltPlan-style flight planning service built to learn .NET Core backend and iOS app
development — covering the key patterns for scalable, well-structured service architecture.

---

## What This Covers

### Backend (.NET Core)
| Concept | Where You'll See It |
|---|---|
| ASP.NET Core Web API setup | `Program.cs` |
| Dependency Injection | `Program.cs` registrations, constructor injection in services/controllers |
| DbContext + EF Core | `Data/AppDbContext.cs` |
| EF Core Migrations | Run commands below |
| LINQ queries | `Services/FlightPlanService.cs` |
| DTOs vs Entities | `Models/` vs `DTOs/` |
| Service layer pattern | `Services/IFlightPlanService.cs` + `FlightPlanService.cs` |
| Controller + routing | `Controllers/FlightPlansController.cs` |
| Async/await throughout | Every service and controller method |
| Model validation | `DTOs/FlightPlanDtos.cs` + controller |
| Global error handling | `Middleware/ErrorHandlingMiddleware.cs` |
| In-memory caching | `Controllers/AirportsController.cs` |
| Seeding data | `Data/SeedData.cs` |

### iOS (Swift)
| Concept | Where You'll See It |
|---|---|
| URLSession async/await | `Services/FlightPlanAPIService.swift` |
| Codable (JSON decode/encode) | `Models/FlightPlan.swift` |
| MVVM pattern | `ViewModels/FlightPlanViewModel.swift` |
| @Published + ObservableObject | `ViewModels/FlightPlanViewModel.swift` |
| SwiftUI List, Navigation, Forms | `Views/` |
| Error handling in Swift | `Services/FlightPlanAPIService.swift` |
| Status transitions from UI | `Views/FlightPlanDetailView.swift` |

---

## Prerequisites

### macOS Tools

Install [Homebrew](https://brew.sh) if you don't have it:

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### Backend

**1. Install .NET 8 SDK**

```bash
brew install dotnet
```

Verify the install:

```bash
dotnet --version   # should print 8.x.x
```

**2. Install the EF Core CLI tool** (one-time, global install)

```bash
dotnet tool install --global dotnet-ef
```

Verify:

```bash
dotnet ef --version
```

> If `dotnet ef` is not found after installing, add `~/.dotnet/tools` to your PATH:
> ```bash
> echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc && source ~/.zshrc
> ```

**3. Install jq** (optional, makes curl output readable)

```bash
brew install jq
```

**IDE options (pick one):**
- [JetBrains Rider](https://www.jetbrains.com/rider/) — recommended for Mac, best .NET experience
- VS Code with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension

### iOS

**Xcode 15 or later** — install from the Mac App Store.

After installing, accept the license and install command-line tools:

```bash
sudo xcodebuild -license accept
xcode-select --install
```

---

## Getting Started

### 1. Run the Backend

```bash
cd backend/MiniFlightPlan.API

# Restore NuGet packages
dotnet restore

# Create the SQLite database from migrations (creates miniflightplan.db)
dotnet ef database update

# Run the API (starts on http://localhost:5000)
dotnet run
```

Open **http://localhost:5000/swagger** to explore and test all endpoints in Swagger UI.

To verify it's working from the terminal:

```bash
curl http://localhost:5000/api/airports | jq
curl http://localhost:5000/api/flightplans | jq
```

See `backend/COMMANDS.sh` for a full set of ready-to-run curl examples covering every endpoint.

### 2. Set Up the iOS App

The repository contains the Swift source files but not an Xcode project file (`.xcodeproj`
is binary and not practical to store in git). You need to create the project once in Xcode
and add the existing source files to it.

**Step 1 — Create a new Xcode project**

1. Open Xcode → **File → New → Project**
2. Choose **iOS → App** → click Next
3. Fill in the fields:
   - Product Name: `FlightPlanApp`
   - Interface: **SwiftUI**
   - Language: **Swift**
   - Minimum Deployments: **iOS 17**
4. Save the project inside `ios/FlightPlanApp/` (the existing directory)

**Step 2 — Add the existing Swift source files**

1. In the Xcode Project Navigator (left panel), delete the default `ContentView.swift`
   that Xcode generated (move to trash)
2. Right-click the `FlightPlanApp` group → **Add Files to "FlightPlanApp"**
3. Navigate to `ios/FlightPlanApp/` and select all `.swift` files and the
   `Models/`, `Services/`, `ViewModels/`, and `Views/` folders
4. Make sure **"Copy items if needed"** is **unchecked** (files already live in the right place)
5. Click Add

**Step 3 — Allow HTTP connections to localhost**

The iOS app connects to `http://localhost:5000` (non-HTTPS). By default iOS blocks plain HTTP.
You need to add an App Transport Security exception:

1. In the Project Navigator, click **Info.plist**
2. Hover over any row → click the **+** button to add a new key
3. Add: `NSAppTransportSecurity` → type **Dictionary**
4. Inside it, add: `NSAllowsLocalNetworking` → type **Boolean** → value **YES**

Alternatively, open `Info.plist` as source and add:

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsLocalNetworking</key>
    <true/>
</dict>
```

**Step 4 — Build and run**

1. Select **iPhone 15** (or any iPhone simulator) from the device picker at the top
2. Press **Cmd+R** to build and run

> The iOS Simulator can reach `localhost` directly.
> On a real physical device you would need to replace `localhost` in
> `FlightPlanAPIService.swift` with your Mac's local IP (e.g. `192.168.1.x`).

---

## The Domain Model

```
Airport
  - ICAO code (e.g. KSEA, KPHX, KORD)
  - Name, City, State

FlightPlan
  - Departure Airport
  - Arrival Airport
  - Aircraft registration (e.g. N12345)
  - Estimated departure time
  - Flight rules (IFR / VFR)
  - Status (Draft → Filed → Active → Closed / Cancelled)
  - Route string (e.g. "SEA V23 PDX")
  - Estimated time en route (minutes)
```

10 U.S. airports and 2 sample flight plans are pre-seeded on first run.

---

## Learning Exercises

These are the next things to add after you've read and understood the existing code.
The existing implementation covers the full working app — these exercises extend it.

### Backend Exercises

1. **Mock weather endpoint**
   Add `GET /api/flightplans/{id}/weather` that returns hardcoded weather for the
   departure airport. Practice adding a new endpoint end-to-end (controller → service → DTO).

2. **Auto-set `filed_at` timestamp**
   When a flight plan's status changes to `Filed`, automatically set `FiledAt = DateTime.UtcNow`
   in the service. Practice EF Core change tracking and service logic.

3. **Multi-field filtering**
   Extend `GET /api/flightplans` to accept both `?status=Filed&departureIcao=KSEA`.
   Practice chaining LINQ `.Where()` conditions based on optional query parameters.

### iOS Exercises

1. **Edit flight plan**
   Add an edit screen that lets the user update the route string or ETE on a Draft plan.
   Practice `PUT` requests and pre-populating a form from existing data.

2. **Offline error state**
   Show a specific "can't reach server" message when the API is not running, distinct from
   other errors. Practice typed error handling in Swift.

---

## Key Concepts Explained

### Why DTOs?
Your EF entities (in `Models/`) map directly to database tables. You don't want to
expose them directly to API callers because:
- They may contain fields you don't want to expose
- Input shape (create request) often differs from output shape (response)
- Decouples your API contract from your database schema

### Why a Service Layer?
Controllers should be thin — they handle HTTP concerns (routing, status codes, validation).
Business logic lives in the service layer. This makes services unit-testable
independently of HTTP.

### Why Interfaces for Services?
`IFlightPlanService` interface + `FlightPlanService` implementation enables:
- Dependency injection (register the interface, inject anywhere)
- Easy unit testing (mock the interface)
- Swappable implementations (e.g. a test double)

### EF Core vs Raw SQL
EF Core lets you work with C# objects and LINQ instead of writing SQL.
The DbContext tracks changes to entities and translates LINQ to SQL automatically.
Migrations are the version-controlled record of how your schema evolves.

### Swift Codable
The Swift equivalent of C# JSON serialization. Any struct/class conforming to
`Codable` can be automatically encoded to / decoded from JSON. The property names
map to JSON keys — just like `System.Text.Json` serialization in .NET.

### MVVM in SwiftUI
- **Model**: The data structs (`FlightPlan.swift`)
- **ViewModel**: `ObservableObject` class with `@Published` properties — the SwiftUI
  views observe these and re-render automatically when they change
- **View**: SwiftUI views that read from the ViewModel, never directly from the API
