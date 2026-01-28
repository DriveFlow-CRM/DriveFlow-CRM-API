using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveFlow_CRM_API.Migrations
{
    /// <inheritdoc />
    public partial class ExamFormLinkedToLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamForms_TeachingCategories_TeachingCategoryId",
                table: "ExamForms");

            migrationBuilder.RenameColumn(
                name: "TeachingCategoryId",
                table: "ExamForms",
                newName: "LicenseId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamForms_TeachingCategoryId",
                table: "ExamForms",
                newName: "IX_ExamForms_LicenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamForms_Licenses_LicenseId",
                table: "ExamForms",
                column: "LicenseId",
                principalTable: "Licenses",
                principalColumn: "LicenseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamForms_Licenses_LicenseId",
                table: "ExamForms");

            migrationBuilder.RenameColumn(
                name: "LicenseId",
                table: "ExamForms",
                newName: "TeachingCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamForms_LicenseId",
                table: "ExamForms",
                newName: "IX_ExamForms_TeachingCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamForms_TeachingCategories_TeachingCategoryId",
                table: "ExamForms",
                column: "TeachingCategoryId",
                principalTable: "TeachingCategories",
                principalColumn: "TeachingCategoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
