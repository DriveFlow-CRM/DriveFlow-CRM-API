using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveFlow_CRM_API.Migrations
{
    /// <inheritdoc />
    public partial class AddFormularAndItemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Formulars",
                columns: table => new
                {
                    FormularId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TeachingCategoryId = table.Column<int>(type: "int", nullable: false),
                    MaxPoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formulars", x => x.FormularId);
                    table.ForeignKey(
                        name: "FK_Formulars_TeachingCategories_TeachingCategoryId",
                        column: x => x.TeachingCategoryId,
                        principalTable: "TeachingCategories",
                        principalColumn: "TeachingCategoryId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormularId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PenaltyPoints = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_Items_Formulars_FormularId",
                        column: x => x.FormularId,
                        principalTable: "Formulars",
                        principalColumn: "FormularId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Formulars_TeachingCategoryId",
                table: "Formulars",
                column: "TeachingCategoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_FormularId_Description",
                table: "Items",
                columns: new[] { "FormularId", "Description" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_FormularId",
                table: "Items",
                column: "FormularId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Formulars");
        }
    }
}
