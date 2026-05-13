using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tunav_backend.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePartnerIdWithUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_partner_claims_partners_PartnerId",
                table: "partner_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_demands_partners_PartnerId",
                table: "partner_demands");

            migrationBuilder.RenameColumn(
                name: "PartnerId",
                table: "partner_demands",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_partner_demands_PartnerId",
                table: "partner_demands",
                newName: "IX_partner_demands_UserId");

            migrationBuilder.RenameColumn(
                name: "PartnerId",
                table: "partner_claims",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_partner_claims_PartnerId",
                table: "partner_claims",
                newName: "IX_partner_claims_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_partner_claims_users_UserId",
                table: "partner_claims",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_partner_demands_users_UserId",
                table: "partner_demands",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_partner_claims_users_UserId",
                table: "partner_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_demands_users_UserId",
                table: "partner_demands");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "partner_demands",
                newName: "PartnerId");

            migrationBuilder.RenameIndex(
                name: "IX_partner_demands_UserId",
                table: "partner_demands",
                newName: "IX_partner_demands_PartnerId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "partner_claims",
                newName: "PartnerId");

            migrationBuilder.RenameIndex(
                name: "IX_partner_claims_UserId",
                table: "partner_claims",
                newName: "IX_partner_claims_PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_partner_claims_partners_PartnerId",
                table: "partner_claims",
                column: "PartnerId",
                principalTable: "partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_partner_demands_partners_PartnerId",
                table: "partner_demands",
                column: "PartnerId",
                principalTable: "partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
