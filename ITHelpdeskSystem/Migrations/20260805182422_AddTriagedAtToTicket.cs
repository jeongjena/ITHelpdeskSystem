using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHelpdeskSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTriagedAtToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TriagedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriagedAt",
                table: "Tickets");
        }
    }
}
