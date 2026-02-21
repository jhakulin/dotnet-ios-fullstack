### EF Core Migration Commands
# Run these from: backend/MiniFlightPlan.API/

# Install EF Core tools globally (one-time)
dotnet tool install --global dotnet-ef

# Create initial migration (generates the SQL schema from your models)
dotnet ef migrations add InitialCreate

# Apply migrations to create/update the SQLite database
dotnet ef database update

# If you change a Model (add a property, new entity, etc.):
dotnet ef migrations add <DescriptiveName>   # e.g. AddFiledAtToFlightPlan
dotnet ef database update

# View the generated SQL without applying it
dotnet ef migrations script

# Remove last migration (if not yet applied)
dotnet ef migrations remove


### Useful dotnet CLI Commands

# Run the app
dotnet run

# Run with hot reload (recompiles on file save)
dotnet watch run

# Build only
dotnet build

# Restore NuGet packages
dotnet restore


### Test the API with curl

# Get all flight plans
curl http://localhost:5000/api/flightplans | jq

# Get filtered by status
curl "http://localhost:5000/api/flightplans?status=Filed" | jq

# Get single flight plan
curl http://localhost:5000/api/flightplans/1 | jq

# Get all airports
curl http://localhost:5000/api/airports | jq

# Create a flight plan (get airport IDs from the airports endpoint first)
curl -X POST http://localhost:5000/api/flightplans \
  -H "Content-Type: application/json" \
  -d '{
    "aircraftRegistration": "N99999",
    "departureAirportId": 1,
    "arrivalAirportId": 2,
    "estimatedDepartureTime": "2026-03-01T14:00:00Z",
    "eteMinutes": 120,
    "route": "KSEA SEA J80 KPHX",
    "flightRules": "IFR"
  }' | jq

# Update status to Filed
curl -X PATCH http://localhost:5000/api/flightplans/1/status \
  -H "Content-Type: application/json" \
  -d '{"newStatus": "Filed"}' | jq

# Delete a draft flight plan
curl -X DELETE http://localhost:5000/api/flightplans/3

# Open Swagger UI in browser
open http://localhost:5000/swagger
