using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang.Migrations
{
    public partial class AddProductContentNote : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentNote",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ContentNote", table: "Products");
        }
    }
}
