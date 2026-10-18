using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIamUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                schema: "iam",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    login_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_parties_party_id",
                        column: x => x.party_id,
                        principalSchema: "iam",
                        principalTable: "parties",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_party_id",
                schema: "iam",
                table: "users",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_login",
                schema: "iam",
                table: "users",
                columns: new[] { "tenant_id", "login_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users",
                schema: "iam");
        }
    }
}
