using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vectis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockLowEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiringSoonEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PreparedBottleAgingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StockLowBottleThreshold = table.Column<int>(type: "integer", nullable: false),
                    ExpiringSoonHours = table.Column<int>(type: "integer", nullable: false),
                    PreparedBottleAgeMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.FamilyId);
                    table.CheckConstraint("CK_notification_preferences_bottle_age_positive", "\"PreparedBottleAgeMinutes\" > 0");
                    table.CheckConstraint("CK_notification_preferences_expiring_hours_positive", "\"ExpiringSoonHours\" > 0");
                    table.CheckConstraint("CK_notification_preferences_stock_threshold_positive", "\"StockLowBottleThreshold\" >= 0");
                    table.ForeignKey(
                        name: "FK_notification_preferences_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_FamilyId_Kind_CreatedAt",
                table: "notification_deliveries",
                columns: new[] { "FamilyId", "Kind", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_preferences");
        }
    }
}
