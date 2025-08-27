using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.TodoData.Migrations
{
    /// <inheritdoc />
    public partial class InitTodoContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cprs",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CprNr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cprs", x => x.UserId);
                    table.UniqueConstraint("AK_Cprs_CprNr", x => x.CprNr);
                    table.CheckConstraint("CK_Cpr_CprNr_Format", "\"CprNr\" ~ '^[0-9]{10}$'");
                });

            migrationBuilder.CreateTable(
                name: "Todos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CprNr = table.Column<string>(type: "character varying(10)", nullable: false),
                    Item = table.Column<string>(type: "text", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Todos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Todos_Cprs_CprNr",
                        column: x => x.CprNr,
                        principalTable: "Cprs",
                        principalColumn: "CprNr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cprs_CprNr",
                table: "Cprs",
                column: "CprNr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Todos_CprNr",
                table: "Todos",
                column: "CprNr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Todos");

            migrationBuilder.DropTable(
                name: "Cprs");
        }
    }
}
