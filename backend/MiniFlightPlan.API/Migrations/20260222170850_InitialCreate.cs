using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniFlightPlan.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IcaoCode = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AircraftRegistration = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DepartureAirportId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArrivalAirportId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedDepartureTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EteMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FlightRules = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FiledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightPlans_Airports_ArrivalAirportId",
                        column: x => x.ArrivalAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightPlans_Airports_DepartureAirportId",
                        column: x => x.DepartureAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Airports_IcaoCode",
                table: "Airports",
                column: "IcaoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightPlans_ArrivalAirportId",
                table: "FlightPlans",
                column: "ArrivalAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightPlans_DepartureAirportId",
                table: "FlightPlans",
                column: "DepartureAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightPlans_EstimatedDepartureTime",
                table: "FlightPlans",
                column: "EstimatedDepartureTime");

            migrationBuilder.CreateIndex(
                name: "IX_FlightPlans_Status",
                table: "FlightPlans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightPlans");

            migrationBuilder.DropTable(
                name: "Airports");
        }
    }
}
