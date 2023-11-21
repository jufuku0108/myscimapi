using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyScimAPI.Data.Migrations
{
    public partial class AddScimGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScimGroup",
                columns: table => new
                {
                    ScimGroupId = table.Column<Guid>(nullable: false),
                    Schemas = table.Column<string>(nullable: true),
                    ExternalId = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimGroup", x => x.ScimGroupId);
                });

            migrationBuilder.CreateTable(
                name: "ScimGroupMember",
                columns: table => new
                {
                    ScimGroupMemberId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(nullable: true),
                    Display = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    Ref = table.Column<string>(nullable: true),
                    ScimGroupId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimGroupMember", x => x.ScimGroupMemberId);
                    table.ForeignKey(
                        name: "FK_ScimGroupMember_ScimGroup_ScimGroupId",
                        column: x => x.ScimGroupId,
                        principalTable: "ScimGroup",
                        principalColumn: "ScimGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimGroupMeta",
                columns: table => new
                {
                    ScimGroupMetaId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceType = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    Version = table.Column<string>(nullable: true),
                    Location = table.Column<string>(nullable: true),
                    ScimGroupId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimGroupMeta", x => x.ScimGroupMetaId);
                    table.ForeignKey(
                        name: "FK_ScimGroupMeta_ScimGroup_ScimGroupId",
                        column: x => x.ScimGroupId,
                        principalTable: "ScimGroup",
                        principalColumn: "ScimGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScimGroupMember_ScimGroupId",
                table: "ScimGroupMember",
                column: "ScimGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimGroupMeta_ScimGroupId",
                table: "ScimGroupMeta",
                column: "ScimGroupId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScimGroupMember");

            migrationBuilder.DropTable(
                name: "ScimGroupMeta");

            migrationBuilder.DropTable(
                name: "ScimGroup");
        }
    }
}
