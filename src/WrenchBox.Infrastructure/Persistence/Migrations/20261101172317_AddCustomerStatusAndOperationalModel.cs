using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WrenchBox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerStatusAndOperationalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE customers SET "DocumentType" = 1 WHERE length("Document") = 14;
                """);

            migrationBuilder.CreateTable(
                name: "integration_failures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Integration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_failures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_BudgetSentAt",
                table: "work_orders",
                column: "BudgetSentAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_CompletedAt",
                table: "work_orders",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_DeliveredAt",
                table: "work_orders",
                column: "DeliveredAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_DiagnosisStartedAt",
                table: "work_orders",
                column: "DiagnosisStartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_ExecutionStartedAt",
                table: "work_orders",
                column: "ExecutionStartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_Status_CreatedAt",
                table: "work_orders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_WorkOrderId",
                table: "stock_movements",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Status",
                table: "customers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_integration_failures_OccurredAt",
                table: "integration_failures",
                column: "OccurredAt");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_work_orders_WorkOrderId",
                table: "stock_movements",
                column: "WorkOrderId",
                principalTable: "work_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_work_orders_WorkOrderId",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "integration_failures");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_BudgetSentAt",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_CompletedAt",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_DeliveredAt",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_DiagnosisStartedAt",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_ExecutionStartedAt",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_Status_CreatedAt",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_WorkOrderId",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_customers_Status",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "customers");
        }
    }
}
