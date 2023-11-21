using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyScimAPI.Data.Migrations
{
    public partial class AddEntitlementAndRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserX509Certificate",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserX509Certificate",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserX509Certificate",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ScimUserX509Certificate",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserPhoto",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserPhoto",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserPhoto",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserPhoneNumber",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserPhoneNumber",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserPhoneNumber",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserIm",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserIm",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserIm",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Primary",
                table: "ScimUserGroup",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ScimUserGroup",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserEmail",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserEmail",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Display",
                table: "ScimUserAddress",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "ScimUserAddress",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ScimUserAddress",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScimUserEntitlement",
                columns: table => new
                {
                    ScimUserEntitlementId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(nullable: true),
                    Primary = table.Column<bool>(nullable: false),
                    Display = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    Ref = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserEntitlement", x => x.ScimUserEntitlementId);
                    table.ForeignKey(
                        name: "FK_ScimUserEntitlement_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserRole",
                columns: table => new
                {
                    ScimUserRoleId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(nullable: true),
                    Primary = table.Column<bool>(nullable: false),
                    Display = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    Ref = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserRole", x => x.ScimUserRoleId);
                    table.ForeignKey(
                        name: "FK_ScimUserRole_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserEntitlement_ScimUserId",
                table: "ScimUserEntitlement",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserRole_ScimUserId",
                table: "ScimUserRole",
                column: "ScimUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScimUserEntitlement");

            migrationBuilder.DropTable(
                name: "ScimUserRole");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserX509Certificate");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserX509Certificate");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserX509Certificate");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ScimUserX509Certificate");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserPhoto");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserPhoto");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserPhoto");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserPhoneNumber");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserPhoneNumber");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserPhoneNumber");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserIm");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserIm");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserIm");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "ScimUserGroup");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ScimUserGroup");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserEmail");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserEmail");

            migrationBuilder.DropColumn(
                name: "Display",
                table: "ScimUserAddress");

            migrationBuilder.DropColumn(
                name: "Ref",
                table: "ScimUserAddress");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ScimUserAddress");
        }
    }
}
