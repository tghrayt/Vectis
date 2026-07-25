using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vectis.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationalConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ContainerId",
                table: "stock_movements",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_UserId",
                table: "stock_movements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_pumping_sessions_BabyId",
                table: "pumping_sessions",
                column: "BabyId");

            migrationBuilder.CreateIndex(
                name: "IX_pumping_sessions_CreatedByUserId",
                table: "pumping_sessions",
                column: "CreatedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_pumping_sessions_total_positive",
                table: "pumping_sessions",
                sql: "\"TotalQuantityMl\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_prepared_bottles_BabyId",
                table: "prepared_bottles",
                column: "BabyId");

            migrationBuilder.CreateIndex(
                name: "IX_prepared_bottles_PreparedByUserId",
                table: "prepared_bottles",
                column: "PreparedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_prepared_bottles_total_positive",
                table: "prepared_bottles",
                sql: "\"TotalQuantityMl\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_prepared_bottle_sources_ContainerId",
                table: "prepared_bottle_sources",
                column: "ContainerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_prepared_bottle_sources_quantity_positive",
                table: "prepared_bottle_sources",
                sql: "\"QuantityMl\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_milk_containers_PumpingSessionId",
                table: "milk_containers",
                column: "PumpingSessionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_milk_containers_initial_positive",
                table: "milk_containers",
                sql: "\"InitialQuantityMl\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_milk_containers_remaining_range",
                table: "milk_containers",
                sql: "\"RemainingQuantityMl\" >= 0 AND \"RemainingQuantityMl\" <= \"InitialQuantityMl\"");

            migrationBuilder.CreateIndex(
                name: "IX_feedings_BabyId",
                table: "feedings",
                column: "BabyId");

            migrationBuilder.CreateIndex(
                name: "IX_feedings_FedByUserId",
                table: "feedings",
                column: "FedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_feedings_PreparedBottleId",
                table: "feedings",
                column: "PreparedBottleId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedings_consumed_range",
                table: "feedings",
                sql: "\"ConsumedQuantityMl\" >= 0 AND \"ConsumedQuantityMl\" <= \"PreparedQuantityMl\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedings_leftover_non_negative",
                table: "feedings",
                sql: "\"LeftoverQuantityMl\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedings_prepared_positive",
                table: "feedings",
                sql: "\"PreparedQuantityMl\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_FamilyId",
                table: "family_members",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_families_CreatorUserId",
                table: "families",
                column: "CreatorUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_conservation_rules_duration_positive",
                table: "conservation_rules",
                sql: "\"DurationHours\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_babies_FamilyId",
                table: "babies",
                column: "FamilyId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_babies_usual_bottle_positive",
                table: "babies",
                sql: "\"UsualBottleMl\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_UserId",
                table: "audit_entries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_entries_users_UserId",
                table: "audit_entries",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_babies_families_FamilyId",
                table: "babies",
                column: "FamilyId",
                principalTable: "families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_families_users_CreatorUserId",
                table: "families",
                column: "CreatorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_family_members_families_FamilyId",
                table: "family_members",
                column: "FamilyId",
                principalTable: "families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_family_members_users_UserId",
                table: "family_members",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_feedings_babies_BabyId",
                table: "feedings",
                column: "BabyId",
                principalTable: "babies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_feedings_prepared_bottles_PreparedBottleId",
                table: "feedings",
                column: "PreparedBottleId",
                principalTable: "prepared_bottles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_feedings_users_FedByUserId",
                table: "feedings",
                column: "FedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_milk_containers_babies_BabyId",
                table: "milk_containers",
                column: "BabyId",
                principalTable: "babies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_milk_containers_pumping_sessions_PumpingSessionId",
                table: "milk_containers",
                column: "PumpingSessionId",
                principalTable: "pumping_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prepared_bottle_sources_milk_containers_ContainerId",
                table: "prepared_bottle_sources",
                column: "ContainerId",
                principalTable: "milk_containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prepared_bottle_sources_prepared_bottles_PreparedBottleId",
                table: "prepared_bottle_sources",
                column: "PreparedBottleId",
                principalTable: "prepared_bottles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prepared_bottles_babies_BabyId",
                table: "prepared_bottles",
                column: "BabyId",
                principalTable: "babies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_prepared_bottles_users_PreparedByUserId",
                table: "prepared_bottles",
                column: "PreparedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pumping_sessions_babies_BabyId",
                table: "pumping_sessions",
                column: "BabyId",
                principalTable: "babies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pumping_sessions_users_CreatedByUserId",
                table: "pumping_sessions",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_milk_containers_ContainerId",
                table: "stock_movements",
                column: "ContainerId",
                principalTable: "milk_containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_users_UserId",
                table: "stock_movements",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_entries_users_UserId",
                table: "audit_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_babies_families_FamilyId",
                table: "babies");

            migrationBuilder.DropForeignKey(
                name: "FK_families_users_CreatorUserId",
                table: "families");

            migrationBuilder.DropForeignKey(
                name: "FK_family_members_families_FamilyId",
                table: "family_members");

            migrationBuilder.DropForeignKey(
                name: "FK_family_members_users_UserId",
                table: "family_members");

            migrationBuilder.DropForeignKey(
                name: "FK_feedings_babies_BabyId",
                table: "feedings");

            migrationBuilder.DropForeignKey(
                name: "FK_feedings_prepared_bottles_PreparedBottleId",
                table: "feedings");

            migrationBuilder.DropForeignKey(
                name: "FK_feedings_users_FedByUserId",
                table: "feedings");

            migrationBuilder.DropForeignKey(
                name: "FK_milk_containers_babies_BabyId",
                table: "milk_containers");

            migrationBuilder.DropForeignKey(
                name: "FK_milk_containers_pumping_sessions_PumpingSessionId",
                table: "milk_containers");

            migrationBuilder.DropForeignKey(
                name: "FK_prepared_bottle_sources_milk_containers_ContainerId",
                table: "prepared_bottle_sources");

            migrationBuilder.DropForeignKey(
                name: "FK_prepared_bottle_sources_prepared_bottles_PreparedBottleId",
                table: "prepared_bottle_sources");

            migrationBuilder.DropForeignKey(
                name: "FK_prepared_bottles_babies_BabyId",
                table: "prepared_bottles");

            migrationBuilder.DropForeignKey(
                name: "FK_prepared_bottles_users_PreparedByUserId",
                table: "prepared_bottles");

            migrationBuilder.DropForeignKey(
                name: "FK_pumping_sessions_babies_BabyId",
                table: "pumping_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_pumping_sessions_users_CreatedByUserId",
                table: "pumping_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_milk_containers_ContainerId",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_users_UserId",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_ContainerId",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_UserId",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_pumping_sessions_BabyId",
                table: "pumping_sessions");

            migrationBuilder.DropIndex(
                name: "IX_pumping_sessions_CreatedByUserId",
                table: "pumping_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_pumping_sessions_total_positive",
                table: "pumping_sessions");

            migrationBuilder.DropIndex(
                name: "IX_prepared_bottles_BabyId",
                table: "prepared_bottles");

            migrationBuilder.DropIndex(
                name: "IX_prepared_bottles_PreparedByUserId",
                table: "prepared_bottles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_prepared_bottles_total_positive",
                table: "prepared_bottles");

            migrationBuilder.DropIndex(
                name: "IX_prepared_bottle_sources_ContainerId",
                table: "prepared_bottle_sources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_prepared_bottle_sources_quantity_positive",
                table: "prepared_bottle_sources");

            migrationBuilder.DropIndex(
                name: "IX_milk_containers_PumpingSessionId",
                table: "milk_containers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_milk_containers_initial_positive",
                table: "milk_containers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_milk_containers_remaining_range",
                table: "milk_containers");

            migrationBuilder.DropIndex(
                name: "IX_feedings_BabyId",
                table: "feedings");

            migrationBuilder.DropIndex(
                name: "IX_feedings_FedByUserId",
                table: "feedings");

            migrationBuilder.DropIndex(
                name: "IX_feedings_PreparedBottleId",
                table: "feedings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedings_consumed_range",
                table: "feedings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedings_leftover_non_negative",
                table: "feedings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedings_prepared_positive",
                table: "feedings");

            migrationBuilder.DropIndex(
                name: "IX_family_members_FamilyId",
                table: "family_members");

            migrationBuilder.DropIndex(
                name: "IX_families_CreatorUserId",
                table: "families");

            migrationBuilder.DropCheckConstraint(
                name: "CK_conservation_rules_duration_positive",
                table: "conservation_rules");

            migrationBuilder.DropIndex(
                name: "IX_babies_FamilyId",
                table: "babies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_babies_usual_bottle_positive",
                table: "babies");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_UserId",
                table: "audit_entries");
        }
    }
}
