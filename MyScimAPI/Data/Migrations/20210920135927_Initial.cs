using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyScimAPI.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScimUser",
                columns: table => new
                {
                    ScimUserId = table.Column<Guid>(nullable: false),
                    Schemas = table.Column<string>(nullable: true),
                    ExternalId = table.Column<string>(nullable: true),
                    UserName = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    NickName = table.Column<string>(nullable: true),
                    ProfileUrl = table.Column<string>(nullable: true),
                    UserType = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    PreferredLanguage = table.Column<string>(nullable: true),
                    Locale = table.Column<string>(nullable: true),
                    TimeZone = table.Column<string>(nullable: true),
                    Active = table.Column<bool>(nullable: false),
                    Password = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUser", x => x.ScimUserId);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserAddress",
                columns: table => new
                {
                    ScimUserAddressId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(nullable: true),
                    StreetAddress = table.Column<string>(nullable: true),
                    Locality = table.Column<string>(nullable: true),
                    Region = table.Column<string>(nullable: true),
                    PostalCode = table.Column<string>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    Formatted = table.Column<string>(nullable: true),
                    Primary = table.Column<bool>(nullable: false),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserAddress", x => x.ScimUserAddressId);
                    table.ForeignKey(
                        name: "FK_ScimUserAddress_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserEmail",
                columns: table => new
                {
                    ScimUserEmailId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    Primary = table.Column<bool>(nullable: false),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserEmail", x => x.ScimUserEmailId);
                    table.ForeignKey(
                        name: "FK_ScimUserEmail_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserEnterpriseUser",
                columns: table => new
                {
                    ScimUserEnterpriseUserId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeNumber = table.Column<int>(nullable: false),
                    CostCenter = table.Column<int>(nullable: false),
                    Organization = table.Column<string>(nullable: true),
                    Division = table.Column<string>(nullable: true),
                    Department = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserEnterpriseUser", x => x.ScimUserEnterpriseUserId);
                    table.ForeignKey(
                        name: "FK_ScimUserEnterpriseUser_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserGroup",
                columns: table => new
                {
                    ScimUserGroupId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    Ref = table.Column<string>(nullable: true),
                    Display = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserGroup", x => x.ScimUserGroupId);
                    table.ForeignKey(
                        name: "FK_ScimUserGroup_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserIm",
                columns: table => new
                {
                    ScimUserImId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserIm", x => x.ScimUserImId);
                    table.ForeignKey(
                        name: "FK_ScimUserIm_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserMeta",
                columns: table => new
                {
                    ScimUserMetaId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceType = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    Version = table.Column<string>(nullable: true),
                    Location = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserMeta", x => x.ScimUserMetaId);
                    table.ForeignKey(
                        name: "FK_ScimUserMeta_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserName",
                columns: table => new
                {
                    ScimUserNameId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Formatted = table.Column<string>(nullable: true),
                    FamilyName = table.Column<string>(nullable: true),
                    GivenName = table.Column<string>(nullable: true),
                    MiddleName = table.Column<string>(nullable: true),
                    HonorificPrefix = table.Column<string>(nullable: true),
                    HonorificSuffix = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserName", x => x.ScimUserNameId);
                    table.ForeignKey(
                        name: "FK_ScimUserName_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserPhoneNumber",
                columns: table => new
                {
                    ScimUserPhoneNumberId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserPhoneNumber", x => x.ScimUserPhoneNumberId);
                    table.ForeignKey(
                        name: "FK_ScimUserPhoneNumber_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserPhoto",
                columns: table => new
                {
                    ScimUserPhotoId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserPhoto", x => x.ScimUserPhotoId);
                    table.ForeignKey(
                        name: "FK_ScimUserPhoto_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserX509Certificate",
                columns: table => new
                {
                    ScimUserX509CertificateId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    ScimUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserX509Certificate", x => x.ScimUserX509CertificateId);
                    table.ForeignKey(
                        name: "FK_ScimUserX509Certificate_ScimUser_ScimUserId",
                        column: x => x.ScimUserId,
                        principalTable: "ScimUser",
                        principalColumn: "ScimUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScimUserManager",
                columns: table => new
                {
                    ScimUserManagerId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(nullable: true),
                    Ref = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    ScimUserEnterpriseUserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScimUserManager", x => x.ScimUserManagerId);
                    table.ForeignKey(
                        name: "FK_ScimUserManager_ScimUserEnterpriseUser_ScimUserEnterpriseUserId",
                        column: x => x.ScimUserEnterpriseUserId,
                        principalTable: "ScimUserEnterpriseUser",
                        principalColumn: "ScimUserEnterpriseUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserAddress_ScimUserId",
                table: "ScimUserAddress",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserEmail_ScimUserId",
                table: "ScimUserEmail",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserEnterpriseUser_ScimUserId",
                table: "ScimUserEnterpriseUser",
                column: "ScimUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserGroup_ScimUserId",
                table: "ScimUserGroup",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserIm_ScimUserId",
                table: "ScimUserIm",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserManager_ScimUserEnterpriseUserId",
                table: "ScimUserManager",
                column: "ScimUserEnterpriseUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserMeta_ScimUserId",
                table: "ScimUserMeta",
                column: "ScimUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserName_ScimUserId",
                table: "ScimUserName",
                column: "ScimUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserPhoneNumber_ScimUserId",
                table: "ScimUserPhoneNumber",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserPhoto_ScimUserId",
                table: "ScimUserPhoto",
                column: "ScimUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScimUserX509Certificate_ScimUserId",
                table: "ScimUserX509Certificate",
                column: "ScimUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScimUserAddress");

            migrationBuilder.DropTable(
                name: "ScimUserEmail");

            migrationBuilder.DropTable(
                name: "ScimUserGroup");

            migrationBuilder.DropTable(
                name: "ScimUserIm");

            migrationBuilder.DropTable(
                name: "ScimUserManager");

            migrationBuilder.DropTable(
                name: "ScimUserMeta");

            migrationBuilder.DropTable(
                name: "ScimUserName");

            migrationBuilder.DropTable(
                name: "ScimUserPhoneNumber");

            migrationBuilder.DropTable(
                name: "ScimUserPhoto");

            migrationBuilder.DropTable(
                name: "ScimUserX509Certificate");

            migrationBuilder.DropTable(
                name: "ScimUserEnterpriseUser");

            migrationBuilder.DropTable(
                name: "ScimUser");
        }
    }
}
