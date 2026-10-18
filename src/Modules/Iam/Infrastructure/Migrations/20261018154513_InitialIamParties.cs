using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIamParties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parties",
                schema: "iam",
                columns: table => new
                {
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parties", x => x.party_id);
                });

            migrationBuilder.CreateTable(
                name: "party_consent",
                schema: "iam",
                columns: table => new
                {
                    consent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_kind = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    purpose = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_consent", x => x.consent_id);
                    table.ForeignKey(
                        name: "FK_party_consent_parties_party_id",
                        column: x => x.party_id,
                        principalSchema: "iam",
                        principalTable: "parties",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "party_identifier",
                schema: "iam",
                columns: table => new
                {
                    type_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    number = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_identifier", x => new { x.party_id, x.type_code, x.number });
                    table.ForeignKey(
                        name: "FK_party_identifier_parties_party_id",
                        column: x => x.party_id,
                        principalSchema: "iam",
                        principalTable: "parties",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "party_role",
                schema: "iam",
                columns: table => new
                {
                    party_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    role_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    attached_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_role", x => x.party_role_id);
                    table.ForeignKey(
                        name: "FK_party_role_parties_party_id",
                        column: x => x.party_id,
                        principalSchema: "iam",
                        principalTable: "parties",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_party_consent_party_id",
                schema: "iam",
                table: "party_consent",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_party_identifier_tuple",
                schema: "iam",
                table: "party_identifier",
                columns: new[] { "type_code", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_role_party_id",
                schema: "iam",
                table: "party_role",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_party_role_tenant_role",
                schema: "iam",
                table: "party_role",
                columns: new[] { "tenant_id", "role_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "party_consent",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "party_identifier",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "party_role",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "parties",
                schema: "iam");
        }
    }
}
