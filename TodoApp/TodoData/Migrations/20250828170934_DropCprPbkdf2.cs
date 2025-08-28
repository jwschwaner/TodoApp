using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.TodoData.Migrations
{
    /// <inheritdoc />
    public partial class DropCprPbkdf2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CprPbkdf2",
                table: "Cprs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CprPbkdf2",
                table: "Cprs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
