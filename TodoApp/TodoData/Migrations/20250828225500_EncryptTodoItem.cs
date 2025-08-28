using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.TodoData.Migrations
{
    /// <inheritdoc />
    public partial class EncryptTodoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Item",
                table: "Todos");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedItem",
                table: "Todos",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedItem",
                table: "Todos");

            migrationBuilder.AddColumn<string>(
                name: "Item",
                table: "Todos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
