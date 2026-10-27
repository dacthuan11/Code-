using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.AuctionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BidderEmail",
                table: "Bids",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SellerEmail",
                table: "Auctions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BidderEmail",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "SellerEmail",
                table: "Auctions");
        }
    }
}
