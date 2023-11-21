using Microsoft.EntityFrameworkCore.Migrations;

namespace MyScimAPI.Data.Migrations
{
    public partial class ModifySchema01 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserX509Certificate");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserRole");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserPhoto");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserPhoneNumber");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserIm");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserGroup");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserEntitlement");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserEmail");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserAddress");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserAddress");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserAddress");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ScimUserAddress");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "ScimUserEnterpriseUser",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CostCenter",
                table: "ScimUserEnterpriseUser",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserX509Certificate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserRole",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserPhoto",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserPhoneNumber",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserIm",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserGroup",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserEntitlement",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeNumber",
                table: "ScimUserEnterpriseUser",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CostCenter",
                table: "ScimUserEnterpriseUser",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserEmail",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserAddress",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserAddress",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserAddress",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ScimUserAddress",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
