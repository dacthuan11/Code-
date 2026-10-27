using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.CommerceService.Infrastructure.Migrations
{
    public partial class AddImageUrlToCartItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CartItems",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CartItems");
        }
    }
}
