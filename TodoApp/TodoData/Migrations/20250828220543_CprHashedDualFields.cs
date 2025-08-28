using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.TodoData.Migrations
{
    /// <inheritdoc />
    public partial class CprHashedDualFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Todos_Cprs_CprNr",
                table: "Todos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Cprs_CprNr",
                table: "Cprs");

            migrationBuilder.DropIndex(
                name: "IX_Cprs_CprNr",
                table: "Cprs");

            migrationBuilder.DropColumn(
                name: "CprNr",
                table: "Cprs");

            migrationBuilder.AlterColumn<string>(
                name: "CprNr",
                table: "Todos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)");

            migrationBuilder.AddColumn<string>(
                name: "CprBcrypt",
                table: "Cprs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CprPbkdf2",
                table: "Cprs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Cprs_CprPbkdf2",
                table: "Cprs",
                column: "CprPbkdf2");

            migrationBuilder.AddForeignKey(
                name: "FK_Todos_Cprs_CprNr",
                table: "Todos",
                column: "CprNr",
                principalTable: "Cprs",
                principalColumn: "CprPbkdf2",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Todos_Cprs_CprNr",
                table: "Todos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Cprs_CprPbkdf2",
                table: "Cprs");

            migrationBuilder.DropColumn(
                name: "CprBcrypt",
                table: "Cprs");

            migrationBuilder.DropColumn(
                name: "CprPbkdf2",
                table: "Cprs");

            migrationBuilder.AlterColumn<string>(
                name: "CprNr",
                table: "Todos",
                type: "character varying(256)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CprNr",
                table: "Cprs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Cprs_CprNr",
                table: "Cprs",
                column: "CprNr");

            migrationBuilder.CreateIndex(
                name: "IX_Cprs_CprNr",
                table: "Cprs",
                column: "CprNr",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Todos_Cprs_CprNr",
                table: "Todos",
                column: "CprNr",
                principalTable: "Cprs",
                principalColumn: "CprNr",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
