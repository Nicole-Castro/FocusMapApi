using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusMapApi.Migrations
{
    /// <inheritdoc />
    public partial class UnifyUsersTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    professional_license = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_profiles_profiles_professional_id",
                        column: x => x.professional_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "interest_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interest_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_interest_points_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    poor_signal = table.Column<int>(type: "integer", nullable: true),
                    session_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_Sessions_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audio_descriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_of_audio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_of_audio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    id_point_of_interest = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_descriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_audio_descriptions_Sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audio_descriptions_interest_points_id_point_of_interest",
                        column: x => x.id_point_of_interest,
                        principalTable: "interest_points",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "session_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delta_power = table.Column<int>(type: "integer", nullable: true),
                    theta_power = table.Column<int>(type: "integer", nullable: true),
                    low_alpha_power = table.Column<int>(type: "integer", nullable: true),
                    high_alpha_power = table.Column<int>(type: "integer", nullable: true),
                    low_beta_power = table.Column<int>(type: "integer", nullable: true),
                    high_beta_power = table.Column<int>(type: "integer", nullable: true),
                    low_gamma_power = table.Column<int>(type: "integer", nullable: true),
                    middle_gamma_power = table.Column<int>(type: "integer", nullable: true),
                    attention_value = table.Column<int>(type: "integer", nullable: true),
                    meditation_value = table.Column<int>(type: "integer", nullable: true),
                    raw_eeg_value = table.Column<int>(type: "integer", nullable: true),
                    timestamp_of_record = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_data", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_data_Sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audio_descriptions_id_point_of_interest",
                table: "audio_descriptions",
                column: "id_point_of_interest");

            migrationBuilder.CreateIndex(
                name: "IX_audio_descriptions_session_id",
                table: "audio_descriptions",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_interest_points_patient_id",
                table: "interest_points",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_email",
                table: "profiles",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profiles_professional_id",
                table: "profiles",
                column: "professional_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_data_session_id_timestamp_of_record",
                table: "session_data",
                columns: new[] { "session_id", "timestamp_of_record" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_patient_id",
                table: "Sessions",
                column: "patient_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_descriptions");

            migrationBuilder.DropTable(
                name: "session_data");

            migrationBuilder.DropTable(
                name: "interest_points");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "profiles");
        }
    }
}
