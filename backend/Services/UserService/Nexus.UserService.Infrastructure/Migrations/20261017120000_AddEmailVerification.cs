using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.UserService.Infrastructure.Migrations
{
    [Migration("20261017120000_AddEmailVerification")]
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            // Các user đã tạo trước feature này coi như đã xác thực (không có email để gửi lại)
            migrationBuilder.Sql("UPDATE \"Users\" SET \"IsEmailVerified\" = true WHERE \"IsEmailVerified\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsEmailVerified", table: "Users");
            migrationBuilder.DropColumn(name: "EmailVerificationToken", table: "Users");
            migrationBuilder.DropColumn(name: "EmailVerificationTokenExpiry", table: "Users");
        }
    }
}
