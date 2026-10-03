using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang.Migrations
{
    public partial class AddChatMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsFromStaff = table.Column<bool>(type: "bit", nullable: false),
                    IsReadByStaff = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionKey",
                table: "ChatMessages",
                column: "SessionKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChatMessages");
        }
    }
}
