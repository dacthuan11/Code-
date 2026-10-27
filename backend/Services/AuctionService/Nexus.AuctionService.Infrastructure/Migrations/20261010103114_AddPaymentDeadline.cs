using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.AuctionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                table: "Auctions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                table: "Auctions");
        }
    }
}
