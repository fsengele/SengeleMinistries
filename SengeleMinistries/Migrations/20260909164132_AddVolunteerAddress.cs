using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SengeleMinistries.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApartmentOrUnit",
                table: "VolunteerApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "VolunteerApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "VolunteerApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "VolunteerApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "VolunteerApplications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApartmentOrUnit",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "State",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "VolunteerApplications");
        }
    }
}
