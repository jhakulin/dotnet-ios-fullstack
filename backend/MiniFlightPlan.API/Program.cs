// ============================================================
// Program.cs — The entry point and composition root of the app
//
// In .NET 6+ the old Startup.cs is merged into Program.cs.
// This file does two things:
//   1. Register services into the DI container (builder.Services.Add...)
//   2. Configure the middleware pipeline (app.Use...)
//
// DEPENDENCY INJECTION PRIMER:
//   You register a type here once, then any class that needs it
//   just declares it in its constructor — .NET wires it up automatically.
//   Three lifetimes:
//     Singleton  — one instance for the entire app lifetime
//     Scoped     — one instance per HTTP request (most common for services + DbContext)
//     Transient  — new instance every time it's requested
// ============================================================

using Microsoft.EntityFrameworkCore;
using MiniFlightPlan.API.Data;
using MiniFlightPlan.API.Middleware;
using MiniFlightPlan.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. REGISTER SERVICES INTO THE DI CONTAINER ──────────────

// Controllers: scans for classes decorated with [ApiController] and registers them
builder.Services.AddControllers();

// EF Core DbContext with SQLite
// AddDbContext registers AppDbContext as Scoped (one per request — correct for EF)
// The connection string comes from appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Our flight plan service — registered against its interface (IFlightPlanService)
// This is the key DI pattern: callers depend on the interface, not the concrete class.
// Swap the implementation here without touching any other code.
builder.Services.AddScoped<IFlightPlanService, FlightPlanService>();

// In-memory cache — useful for caching the airport list (rarely changes)
builder.Services.AddMemoryCache();

// Swagger — generates OpenAPI spec and interactive UI at /swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    { 
        Title = "MiniFlightPlan API", 
        Version = "v1",
        Description = "A simplified FltPlan-style flight planning service. " +
                      "Mirrors the kind of backend the Garmin Chandler team builds."
    });
    // Include XML comments in Swagger (from /// doc comments on controllers)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

// CORS — allow the iOS simulator and any local client to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── 2. CONFIGURE THE MIDDLEWARE PIPELINE ────────────────────
//
// MIDDLEWARE PIPELINE PRIMER:
//   Each request flows through this pipeline top-to-bottom.
//   Each middleware can: handle the request, pass it to the next middleware,
//   or short-circuit (return a response without going further).
//   Order matters — e.g. error handling must come first to catch exceptions
//   thrown by later middleware.

// Our custom global error handler — catches unhandled exceptions from anywhere downstream
// Must be first so it wraps everything else
app.UseMiddleware<ErrorHandlingMiddleware>();

// Swagger UI only in development (don't expose API docs in production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Route requests to the correct controller action
app.UseRouting();
app.MapControllers();

// ── 3. SEED INITIAL DATA ─────────────────────────────────────
// On startup, ensure the database exists and seed some airports
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates DB from model if it doesn't exist
    SeedData.Initialize(db);
}

app.Run();
