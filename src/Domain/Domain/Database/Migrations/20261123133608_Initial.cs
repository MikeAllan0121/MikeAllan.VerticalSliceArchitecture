using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MikeAllan.VerticalSliceArchitecture.Domain.Database.Migrations;

/// <inheritdoc />
public partial class _20261123133608_Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                OutstandingBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                AmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Lines = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_Invoices_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payment",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payment", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payment_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_CustomerId",
            table: "Invoices",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_Payment_InvoiceId",
            table: "Payment",
            column: "InvoiceId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Payment");

        migrationBuilder.DropTable(
            name: "Invoices");

        migrationBuilder.DropTable(
            name: "Customers");
    }
}
