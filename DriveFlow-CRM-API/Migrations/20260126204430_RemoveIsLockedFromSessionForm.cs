using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveFlow_CRM_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsLockedFromSessionForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "SessionForms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "SessionForms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
