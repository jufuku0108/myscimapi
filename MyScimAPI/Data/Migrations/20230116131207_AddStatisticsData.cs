using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyScimAPI.Data.Migrations
{
    public partial class AddStatisticsData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatisticsData",
                columns: table => new
                {
                    StatisticsDataId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAccessCount = table.Column<int>(nullable: false),
                    TotalUserCount = table.Column<int>(nullable: false),
                    TotalGroupCount = table.Column<int>(nullable: false),
                    TotalGetCount = table.Column<int>(nullable: false),
                    TotalPostCount = table.Column<int>(nullable: false),
                    TotalPatchCount = table.Column<int>(nullable: false),
                    TotalDeleteCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticsData", x => x.StatisticsDataId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatisticsData");
        }
    }
}
