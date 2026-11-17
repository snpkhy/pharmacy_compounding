using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyCompounding.Migrations
{
    /// <inheritdoc />
    public partial class OrderStatusTimeAndReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "status_changed_at",
                table: "orders",
                type: "timestamp without time zone",
                nullable: false,
                // Existing orders: count the time events (after 3 / 10 days) from the moment of this migration,
                // so no order is cancelled right after the update
                defaultValueSql: "LOCALTIMESTAMP");
            migrationBuilder.Sql("ALTER TABLE orders ALTER COLUMN status_changed_at DROP DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "status_reason",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Issued orders were picked up on the planned date; until now orders were rejected only for an expired prescription
            migrationBuilder.Sql("UPDATE orders SET status_changed_at = planned_ready_date + interval '12 hours' WHERE status = 'Issued'");
            migrationBuilder.Sql("UPDATE orders SET status_reason = 'Термін дії рецепта минув' WHERE status = 'Rejected'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status_changed_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "status_reason",
                table: "orders");
        }
    }
}
