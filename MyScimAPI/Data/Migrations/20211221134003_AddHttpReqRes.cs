using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyScimAPI.Data.Migrations
{
    public partial class AddHttpReqRes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HttpObject",
                columns: table => new
                {
                    HttpObjectId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTime = table.Column<DateTime>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    Method = table.Column<string>(nullable: true),
                    StatusCode = table.Column<int>(nullable: false),
                    IpAddress = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Headers = table.Column<string>(nullable: true),
                    Body = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HttpObject", x => x.HttpObjectId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HttpObject");
        }
    }
}
