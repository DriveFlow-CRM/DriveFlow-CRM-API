using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveFlow_CRM_API.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureazaBazaDeDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Cities_CityID",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoSchools_Addresses_AddressID",
                table: "AutoSchools");

            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Counties_CountyID",
                table: "Cities");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AutoSchools_AutoSchoolID",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "AutoSchoolID",
                table: "Requests",
                newName: "AutoSchoolId");

            migrationBuilder.RenameColumn(
                name: "RequestID",
                table: "Requests",
                newName: "RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AutoSchoolID",
                table: "Requests",
                newName: "IX_Requests_AutoSchoolId");

            migrationBuilder.RenameColumn(
                name: "CountyID",
                table: "Counties",
                newName: "CountyId");

            migrationBuilder.RenameColumn(
                name: "CountyID",
                table: "Cities",
                newName: "CountyId");

            migrationBuilder.RenameColumn(
                name: "CityID",
                table: "Cities",
                newName: "CityId");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_CountyID",
                table: "Cities",
                newName: "IX_Cities_CountyId");

            migrationBuilder.RenameColumn(
                name: "AddressID",
                table: "AutoSchools",
                newName: "AddressId");

            migrationBuilder.RenameColumn(
                name: "AutoSchoolID",
                table: "AutoSchools",
                newName: "AutoSchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_AutoSchools_AddressID",
                table: "AutoSchools",
                newName: "IX_AutoSchools_AddressId");

            migrationBuilder.RenameColumn(
                name: "CityID",
                table: "Addresses",
                newName: "CityId");

            migrationBuilder.RenameColumn(
                name: "AddressID",
                table: "Addresses",
                newName: "AddressId");

            migrationBuilder.RenameIndex(
                name: "IX_Addresses_CityID",
                table: "Addresses",
                newName: "IX_Addresses_CityId");

            migrationBuilder.AddColumn<int>(
                name: "AutoSchoolId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InstructorAvailabilities",
                columns: table => new
                {
                    InstructorAvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartHour = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndHour = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorAvailabilities", x => x.InstructorAvailabilityId);
                    table.ForeignKey(
                        name: "FK_InstructorAvailabilities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    LicenseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LicenseType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpirationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.LicenseId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScholarshipPayment = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SessionsPayed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TeachingCategories",
                columns: table => new
                {
                    TeachingCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionCost = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SessionDuration = table.Column<int>(type: "int", nullable: false),
                    ScholarshipPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    AutoSchoolId = table.Column<int>(type: "int", nullable: false),
                    LicenseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingCategories", x => x.TeachingCategoryId);
                    table.ForeignKey(
                        name: "FK_TeachingCategories_AutoSchools_AutoSchoolId",
                        column: x => x.AutoSchoolId,
                        principalTable: "AutoSchools",
                        principalColumn: "AutoSchoolId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeachingCategories_Licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "Licenses",
                        principalColumn: "LicenseId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegistrationNr = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GearboxType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Colour = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ITP_ExpirDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    InsuranceExpirDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RCA_ExpirDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LicenseId = table.Column<int>(type: "int", nullable: false),
                    AutoSchoolID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                    table.ForeignKey(
                        name: "FK_Vehicles_AutoSchools_AutoSchoolID",
                        column: x => x.AutoSchoolID,
                        principalTable: "AutoSchools",
                        principalColumn: "AutoSchoolId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vehicles_Licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "Licenses",
                        principalColumn: "LicenseId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApplicationUserTeachingCategories",
                columns: table => new
                {
                    UserTeachingCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TeachingCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserTeachingCategories", x => x.UserTeachingCategoryId);
                    table.ForeignKey(
                        name: "FK_ApplicationUserTeachingCategories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserTeachingCategories_TeachingCategories_Teachin~",
                        column: x => x.TeachingCategoryId,
                        principalTable: "TeachingCategories",
                        principalColumn: "TeachingCategoryId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScholarshipStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CriminalRecordExpirDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MedicalRecordExpirDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstructorId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    TeachingCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_Files_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Files_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Files_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Files_TeachingCategories_TeachingCategoryId",
                        column: x => x.TeachingCategoryId,
                        principalTable: "TeachingCategories",
                        principalColumn: "TeachingCategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Files_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartHour = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndHour = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_Appointments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AutoSchoolId",
                table: "AspNetUsers",
                column: "AutoSchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserTeachingCategories_TeachingCategoryId",
                table: "ApplicationUserTeachingCategories",
                column: "TeachingCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserTeachingCategories_UserId",
                table: "ApplicationUserTeachingCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_FileId",
                table: "Appointments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_InstructorId",
                table: "Files",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_PaymentId",
                table: "Files",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_StudentId",
                table: "Files",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_TeachingCategoryId",
                table: "Files",
                column: "TeachingCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_VehicleId",
                table: "Files",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorAvailabilities_UserId",
                table: "InstructorAvailabilities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingCategories_AutoSchoolId",
                table: "TeachingCategories",
                column: "AutoSchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingCategories_LicenseId",
                table: "TeachingCategories",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_AutoSchoolID",
                table: "Vehicles",
                column: "AutoSchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicenseId",
                table: "Vehicles",
                column: "LicenseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Cities_CityId",
                table: "Addresses",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AutoSchools_AutoSchoolId",
                table: "AspNetUsers",
                column: "AutoSchoolId",
                principalTable: "AutoSchools",
                principalColumn: "AutoSchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoSchools_Addresses_AddressId",
                table: "AutoSchools",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "AddressId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Counties_CountyId",
                table: "Cities",
                column: "CountyId",
                principalTable: "Counties",
                principalColumn: "CountyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AutoSchools_AutoSchoolId",
                table: "Requests",
                column: "AutoSchoolId",
                principalTable: "AutoSchools",
                principalColumn: "AutoSchoolId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Cities_CityId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AutoSchools_AutoSchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoSchools_Addresses_AddressId",
                table: "AutoSchools");

            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Counties_CountyId",
                table: "Cities");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AutoSchools_AutoSchoolId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "ApplicationUserTeachingCategories");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "InstructorAvailabilities");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "TeachingCategories");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AutoSchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AutoSchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "AutoSchoolId",
                table: "Requests",
                newName: "AutoSchoolID");

            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "Requests",
                newName: "RequestID");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AutoSchoolId",
                table: "Requests",
                newName: "IX_Requests_AutoSchoolID");

            migrationBuilder.RenameColumn(
                name: "CountyId",
                table: "Counties",
                newName: "CountyID");

            migrationBuilder.RenameColumn(
                name: "CountyId",
                table: "Cities",
                newName: "CountyID");

            migrationBuilder.RenameColumn(
                name: "CityId",
                table: "Cities",
                newName: "CityID");

            migrationBuilder.RenameIndex(
                name: "IX_Cities_CountyId",
                table: "Cities",
                newName: "IX_Cities_CountyID");

            migrationBuilder.RenameColumn(
                name: "AddressId",
                table: "AutoSchools",
                newName: "AddressID");

            migrationBuilder.RenameColumn(
                name: "AutoSchoolId",
                table: "AutoSchools",
                newName: "AutoSchoolID");

            migrationBuilder.RenameIndex(
                name: "IX_AutoSchools_AddressId",
                table: "AutoSchools",
                newName: "IX_AutoSchools_AddressID");

            migrationBuilder.RenameColumn(
                name: "CityId",
                table: "Addresses",
                newName: "CityID");

            migrationBuilder.RenameColumn(
                name: "AddressId",
                table: "Addresses",
                newName: "AddressID");

            migrationBuilder.RenameIndex(
                name: "IX_Addresses_CityId",
                table: "Addresses",
                newName: "IX_Addresses_CityID");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Cities_CityID",
                table: "Addresses",
                column: "CityID",
                principalTable: "Cities",
                principalColumn: "CityID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AutoSchools_Addresses_AddressID",
                table: "AutoSchools",
                column: "AddressID",
                principalTable: "Addresses",
                principalColumn: "AddressID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Counties_CountyID",
                table: "Cities",
                column: "CountyID",
                principalTable: "Counties",
                principalColumn: "CountyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AutoSchools_AutoSchoolID",
                table: "Requests",
                column: "AutoSchoolID",
                principalTable: "AutoSchools",
                principalColumn: "AutoSchoolID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
