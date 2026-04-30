using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class CollaborationLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers");

            migrationBuilder.AddColumn<int>(
                name: "ManagerUserId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderName = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamChatMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagerUserId",
                table: "Employees",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_AppUsers_ManagerUserId",
                table: "Employees",
                column: "ManagerUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_AppUsers_ManagerUserId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "TeamChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ManagerUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "Employees");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
