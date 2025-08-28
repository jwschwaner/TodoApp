using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.TodoData.Migrations
{
    /// <inheritdoc />
    public partial class HashCpr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cpr_CprNr_Format",
                table: "Cprs");

            migrationBuilder.AlterColumn<string>(
                name: "CprNr",
                table: "Todos",
                type: "character varying(256)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)");

            migrationBuilder.AlterColumn<string>(
                name: "CprNr",
                table: "Cprs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "CprPbkdf2",
                table: "Cprs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CprPbkdf2",
                table: "Cprs");

            migrationBuilder.AlterColumn<string>(
                name: "CprNr",
                table: "Todos",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)");

            migrationBuilder.AlterColumn<string>(
                name: "CprNr",
                table: "Cprs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cpr_CprNr_Format",
                table: "Cprs",
                sql: "\"CprNr\" ~ '^[0-9]{10}$'");
        }
    }
}
