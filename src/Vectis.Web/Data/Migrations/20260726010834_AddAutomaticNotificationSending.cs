using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vectis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticNotificationSending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomaticEmailEnabled",
                table: "notification_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomaticEmailEnabled",
                table: "notification_preferences");
        }
    }
}
