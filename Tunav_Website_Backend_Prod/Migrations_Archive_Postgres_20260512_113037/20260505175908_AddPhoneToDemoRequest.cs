using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tunav_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneToDemoRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "demo_requests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phone",
                table: "demo_requests");
        }
    }
}
