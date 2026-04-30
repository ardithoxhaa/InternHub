using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProductionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "EmployeeDocuments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "EmployeeDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReturnDate",
                table: "CompanyAssets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CompanyAssets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Assigned");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "AppUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_EmployeeId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ReturnDate",
                table: "CompanyAssets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CompanyAssets");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AppUsers");
        }
    }
}
