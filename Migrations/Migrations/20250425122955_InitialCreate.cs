using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "measurand",
                columns: table => new
                {
                    measurand_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    quantity_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantity_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    symbol = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurand", x => x.measurand_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    role_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.role_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor",
                columns: table => new
                {
                    sensor_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sensor_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deployment_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    measurand_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor", x => x.sensor_id);
                    table.ForeignKey(
                        name: "FK_sensor_measurand_measurand_id",
                        column: x => x.measurand_id,
                        principalTable: "measurand",
                        principalColumn: "measurand_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_salt = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "configuration",
                columns: table => new
                {
                    sensor_id = table.Column<int>(type: "int", nullable: false),
                    latitude = table.Column<float>(type: "float", nullable: true),
                    longitude = table.Column<float>(type: "float", nullable: true),
                    altitude = table.Column<float>(type: "float", nullable: true),
                    orientation = table.Column<int>(type: "int", nullable: true),
                    measurment_frequency = table.Column<int>(type: "int", nullable: true),
                    min_threshold = table.Column<float>(type: "float", nullable: true),
                    max_threshold = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuration", x => x.sensor_id);
                    table.ForeignKey(
                        name: "FK_configuration_sensor_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensor",
                        principalColumn: "sensor_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "measurement",
                columns: table => new
                {
                    measurement_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    value = table.Column<float>(type: "float", nullable: true),
                    sensor_id = table.Column<int>(type: "int", nullable: false),
                    MeasurandId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurement", x => x.measurement_id);
                    table.ForeignKey(
                        name: "FK_measurement_measurand_MeasurandId",
                        column: x => x.MeasurandId,
                        principalTable: "measurand",
                        principalColumn: "measurand_id");
                    table.ForeignKey(
                        name: "FK_measurement_sensor_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensor",
                        principalColumn: "sensor_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor_firmware",
                columns: table => new
                {
                    sensor_id = table.Column<int>(type: "int", nullable: false),
                    firmware_version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_update_date = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_firmware", x => x.sensor_id);
                    table.ForeignKey(
                        name: "FK_sensor_firmware_sensor_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensor",
                        principalColumn: "sensor_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incident",
                columns: table => new
                {
                    incident_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    responder_comments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolved_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    priority = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    responder_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident", x => x.incident_id);
                    table.ForeignKey(
                        name: "FK_incident_user_responder_id",
                        column: x => x.responder_id,
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "maintenance",
                columns: table => new
                {
                    maintenance_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    maintenance_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    maintainer_comments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sensor_id = table.Column<int>(type: "int", nullable: false),
                    maintainer_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance", x => x.maintenance_id);
                    table.ForeignKey(
                        name: "FK_maintenance_sensor_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensor",
                        principalColumn: "sensor_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_maintenance_user_maintainer_id",
                        column: x => x.maintainer_id,
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incident_measurement",
                columns: table => new
                {
                    measurement_id = table.Column<int>(type: "int", nullable: false),
                    incident_id = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_measurement", x => new { x.measurement_id, x.incident_id });
                    table.ForeignKey(
                        name: "FK_incident_measurement_incident_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incident",
                        principalColumn: "incident_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_measurement_measurement_measurement_id",
                        column: x => x.measurement_id,
                        principalTable: "measurement",
                        principalColumn: "measurement_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_incident_responder_id",
                table: "incident",
                column: "responder_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_measurement_incident_id",
                table: "incident_measurement",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_maintainer_id",
                table: "maintenance",
                column: "maintainer_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_sensor_id",
                table: "maintenance",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "IX_measurement_MeasurandId",
                table: "measurement",
                column: "MeasurandId");

            migrationBuilder.CreateIndex(
                name: "IX_measurement_sensor_id",
                table: "measurement",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_measurand_id",
                table: "sensor",
                column: "measurand_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_id",
                table: "user",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuration");

            migrationBuilder.DropTable(
                name: "incident_measurement");

            migrationBuilder.DropTable(
                name: "maintenance");

            migrationBuilder.DropTable(
                name: "sensor_firmware");

            migrationBuilder.DropTable(
                name: "incident");

            migrationBuilder.DropTable(
                name: "measurement");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "sensor");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "measurand");
        }
    }
}
