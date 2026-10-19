using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PharmacyCompounding.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "doctors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    specialization = table.Column<string>(type: "text", nullable: true),
                    institution = table.Column<string>(type: "text", nullable: true),
                    license_number = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    user_type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    personnel_number = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<string>(type: "text", nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    admitted_to_production = table.Column<bool>(type: "boolean", nullable: true),
                    qualification_category = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<string>(type: "text", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    validity_days = table.Column<int>(type: "integer", nullable: false),
                    patient = table.Column<string>(type: "text", nullable: false),
                    formula = table.Column<string>(type: "text", nullable: false),
                    doctor_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescriptions_doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    price_per_unit = table.Column<decimal>(type: "numeric", nullable: false),
                    stock = table.Column<double>(type: "double precision", nullable: false),
                    min_stock = table.Column<double>(type: "double precision", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredients_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    planned_ready_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    prescription_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "manufacturing_acts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<int>(type: "integer", nullable: false),
                    manufactured_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    batch = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    technologist_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manufacturing_acts", x => x.id);
                    table.ForeignKey(
                        name: "fk_manufacturing_acts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_manufacturing_acts_technologists_technologist_id",
                        column: x => x.technologist_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    medicine_name = table.Column<string>(type: "text", nullable: false),
                    dosage_form = table.Column<string>(type: "text", nullable: false),
                    dosage = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    work_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    successful = table.Column<bool>(type: "boolean", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quality_checks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    organoleptic = table.Column<bool>(type: "boolean", nullable: false),
                    physical = table.Column<bool>(type: "boolean", nullable: false),
                    chemical = table.Column<bool>(type: "boolean", nullable: false),
                    result = table.Column<bool>(type: "boolean", nullable: false),
                    manufacturing_act_id = table.Column<int>(type: "integer", nullable: false),
                    pharmacist_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quality_checks", x => x.id);
                    table.ForeignKey(
                        name: "fk_quality_checks_manufacturing_acts_manufacturing_act_id",
                        column: x => x.manufacturing_act_id,
                        principalTable: "manufacturing_acts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_quality_checks_pharmacists_pharmacist_id",
                        column: x => x.pharmacist_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "components",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<double>(type: "double precision", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    order_item_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_components_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_components_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_components_ingredient_id",
                table: "components",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_components_order_item_id",
                table: "components",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_code",
                table: "ingredients",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_warehouse_id",
                table: "ingredients",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturing_acts_order_id",
                table: "manufacturing_acts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturing_acts_technologist_id",
                table: "manufacturing_acts",
                column: "technologist_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_client_id",
                table: "orders",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_number",
                table: "orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_prescription_id",
                table: "orders",
                column: "prescription_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id",
                table: "payments",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_doctor_id",
                table: "prescriptions",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_quality_checks_manufacturing_act_id",
                table: "quality_checks",
                column: "manufacturing_act_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quality_checks_pharmacist_id",
                table: "quality_checks",
                column: "pharmacist_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "components");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "quality_checks");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "manufacturing_acts");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "prescriptions");

            migrationBuilder.DropTable(
                name: "doctors");
        }
    }
}
