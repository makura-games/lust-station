using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class LustPortfolioTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profile_portfolio",
                columns: table => new
                {
                    profile_portfolio_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    distinguishing_features = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    region = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    planet_or_colony = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    address = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    education = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    work_experience = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    marital_status_value = table.Column<byte>(type: "smallint", nullable: false),
                    close_relatives = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    emergency_contact = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    physiological_traits = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    psychological_traits = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    arrest_history = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    conviction_history = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_portfolio", x => x.profile_portfolio_id);
                    table.ForeignKey(
                        name: "FK_profile_portfolio_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_portfolio_profile_id",
                table: "profile_portfolio",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profile_portfolio");
        }
    }
}
