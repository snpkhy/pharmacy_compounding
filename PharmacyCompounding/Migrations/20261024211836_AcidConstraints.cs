using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyCompounding.Migrations
{
    /// <inheritdoc />
    public partial class AcidConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "order_numbers",
                startValue: 1001L);

            // Continue numbering after the orders that already exist
            migrationBuilder.Sql("SELECT setval('order_numbers', GREATEST((SELECT MAX(number) FROM orders), 1000));");

            // xmin is a PostgreSQL system column that every table already has, so it is not added here

            migrationBuilder.AlterColumn<int>(
                name: "number",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('order_numbers')",
                oldClrType: typeof(int),
                oldType: "integer");



            migrationBuilder.CreateIndex(
                name: "ix_warehouses_name",
                table: "warehouses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_number",
                table: "prescriptions",
                column: "number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_prescriptions_validity",
                table: "prescriptions",
                sql: "validity_days BETWEEN 1 AND 365");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_amount",
                table: "payments",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_total",
                table: "orders",
                sql: "total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_items_quantity",
                table: "order_items",
                sql: "quantity > 0 AND work_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ingredients_price",
                table: "ingredients",
                sql: "price_per_unit >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ingredients_stock",
                table: "ingredients",
                sql: "stock >= 0 AND min_stock >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_doctors_license_number",
                table: "doctors",
                column: "license_number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_components_amount",
                table: "components",
                sql: "amount > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_warehouses_name",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "ix_prescriptions_number",
                table: "prescriptions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_prescriptions_validity",
                table: "prescriptions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_amount",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_total",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_items_quantity",
                table: "order_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ingredients_price",
                table: "ingredients");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ingredients_stock",
                table: "ingredients");

            migrationBuilder.DropIndex(
                name: "ix_doctors_license_number",
                table: "doctors");

            migrationBuilder.DropCheckConstraint(
                name: "ck_components_amount",
                table: "components");



            migrationBuilder.DropSequence(
                name: "order_numbers");

            migrationBuilder.AlterColumn<int>(
                name: "number",
                table: "orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValueSql: "nextval('order_numbers')");
        }
    }
}
