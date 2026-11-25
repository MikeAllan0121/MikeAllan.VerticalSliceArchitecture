using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MikeAllan.VerticalSliceArchitecture.Domain.Database.Migrations;

/// <inheritdoc />
public partial class _20261125112221_AddRowVersions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<uint>(
            name: "xmin",
            table: "Invoices",
            type: "xid",
            rowVersion: true,
            nullable: false,
            defaultValue: 0u);

        migrationBuilder.AddColumn<uint>(
            name: "xmin",
            table: "Customers",
            type: "xid",
            rowVersion: true,
            nullable: false,
            defaultValue: 0u);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "xmin",
            table: "Invoices");

        migrationBuilder.DropColumn(
            name: "xmin",
            table: "Customers");
    }
}
