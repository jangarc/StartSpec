using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIamRefreshFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "refresh_families",
                schema: "iam",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    current_refresh_token_sha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    current_refresh_token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    current_access_token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    refresh_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rotated_from_sha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    rotated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_families", x => x.family_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_families_current_sha",
                schema: "iam",
                table: "refresh_families",
                column: "current_refresh_token_sha");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_families_rotated_sha",
                schema: "iam",
                table: "refresh_families",
                column: "rotated_from_sha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_families",
                schema: "iam");
        }
    }
}
