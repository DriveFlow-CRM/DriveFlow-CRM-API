using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveFlow_CRM_API.Migrations
{
    /// <inheritdoc />
    public partial class ModificariBazaDeDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorAvailabilities_AspNetUsers_UserId",
                table: "InstructorAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingCategories_Licenses_LicenseId",
                table: "TeachingCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_AutoSchools_AutoSchoolID",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Licenses_LicenseId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_TeachingCategories_LicenseId",
                table: "TeachingCategories");

            migrationBuilder.DropColumn(
                name: "LicenseId",
                table: "TeachingCategories");

            migrationBuilder.RenameColumn(
                name: "AutoSchoolID",
                table: "Vehicles",
                newName: "AutoSchoolId");

            migrationBuilder.RenameColumn(
                name: "LicenseId",
                table: "Vehicles",
                newName: "TeachingCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_AutoSchoolID",
                table: "Vehicles",
                newName: "IX_Vehicles_AutoSchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_LicenseId",
                table: "Vehicles",
                newName: "IX_Vehicles_TeachingCategoryId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "InstructorAvailabilities",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InstructorAvailabilities_UserId",
                table: "InstructorAvailabilities",
                newName: "IX_InstructorAvailabilities_ApplicationUserId");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "TeachingCategories",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorAvailabilities_AspNetUsers_ApplicationUserId",
                table: "InstructorAvailabilities",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_AutoSchools_AutoSchoolId",
                table: "Vehicles",
                column: "AutoSchoolId",
                principalTable: "AutoSchools",
                principalColumn: "AutoSchoolId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_TeachingCategories_TeachingCategoryId",
                table: "Vehicles",
                column: "TeachingCategoryId",
                principalTable: "TeachingCategories",
                principalColumn: "TeachingCategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorAvailabilities_AspNetUsers_ApplicationUserId",
                table: "InstructorAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_AutoSchools_AutoSchoolId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_TeachingCategories_TeachingCategoryId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "TeachingCategories");

            migrationBuilder.RenameColumn(
                name: "AutoSchoolId",
                table: "Vehicles",
                newName: "AutoSchoolID");

            migrationBuilder.RenameColumn(
                name: "TeachingCategoryId",
                table: "Vehicles",
                newName: "LicenseId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_AutoSchoolId",
                table: "Vehicles",
                newName: "IX_Vehicles_AutoSchoolID");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_TeachingCategoryId",
                table: "Vehicles",
                newName: "IX_Vehicles_LicenseId");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "InstructorAvailabilities",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_InstructorAvailabilities_ApplicationUserId",
                table: "InstructorAvailabilities",
                newName: "IX_InstructorAvailabilities_UserId");

            migrationBuilder.AddColumn<int>(
                name: "LicenseId",
                table: "TeachingCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    LicenseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExpirationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LicenseType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.LicenseId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingCategories_LicenseId",
                table: "TeachingCategories",
                column: "LicenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorAvailabilities_AspNetUsers_UserId",
                table: "InstructorAvailabilities",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingCategories_Licenses_LicenseId",
                table: "TeachingCategories",
                column: "LicenseId",
                principalTable: "Licenses",
                principalColumn: "LicenseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_AutoSchools_AutoSchoolID",
                table: "Vehicles",
                column: "AutoSchoolID",
                principalTable: "AutoSchools",
                principalColumn: "AutoSchoolId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Licenses_LicenseId",
                table: "Vehicles",
                column: "LicenseId",
                principalTable: "Licenses",
                principalColumn: "LicenseId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
