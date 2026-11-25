using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeSparks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spark_cycle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    initial_capacity = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    block_reason = table.Column<string>(type: "text", nullable: true),
                    last_queue_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_dollars = table.Column<decimal>(type: "numeric", nullable: true),
                    provider_observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_stale = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_cycle", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spark_queue_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    offered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    entered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    entry_order = table.Column<long>(type: "bigint", nullable: true),
                    culture_name = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    notice_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_queue_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spark_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    initial_amount = table.Column<int>(type: "integer", nullable: false),
                    refill_amount = table.Column<int>(type: "integer", nullable: false),
                    estimated_unit_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    closing_day = table.Column<int>(type: "integer", nullable: false),
                    renewal_lead_days = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spark_cycle_change",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    previous_capacity = table.Column<int>(type: "integer", nullable: true),
                    new_capacity = table.Column<int>(type: "integer", nullable: true),
                    previous_state = table.Column<int>(type: "integer", nullable: true),
                    new_state = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_cycle_change", x => x.id);
                    table.ForeignKey(
                        name: "FK_spark_cycle_change_spark_cycle_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "spark_cycle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spark_daily_snapshot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    available = table.Column<int>(type: "integer", nullable: false),
                    control_line = table.Column<decimal>(type: "numeric", nullable: false),
                    waiting_initial = table.Column<int>(type: "integer", nullable: false),
                    waiting_refill = table.Column<int>(type: "integer", nullable: false),
                    declined = table.Column<int>(type: "integer", nullable: false),
                    provider_dollars = table.Column<decimal>(type: "numeric", nullable: true),
                    provider_sparks = table.Column<long>(type: "bigint", nullable: true),
                    provider_observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_stale = table.Column<bool>(type: "boolean", nullable: false),
                    report_recipients = table.Column<string[]>(type: "text[]", nullable: false),
                    report_subject = table.Column<string>(type: "text", nullable: true),
                    report_body = table.Column<string>(type: "text", nullable: true),
                    report_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_daily_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_spark_daily_snapshot_spark_cycle_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "spark_cycle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spark_grant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    queue_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    consumed = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_kind = table.Column<int>(type: "integer", nullable: true),
                    reservation_game_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reserved = table.Column<int>(type: "integer", nullable: false),
                    reserved_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    receipts = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spark_grant", x => x.id);
                    table.ForeignKey(
                        name: "FK_spark_grant_spark_cycle_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "spark_cycle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spark_grant_spark_queue_request_queue_request_id",
                        column: x => x.queue_request_id,
                        principalTable: "spark_queue_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_spark_cycle_starts_at",
                table: "spark_cycle",
                column: "starts_at",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spark_cycle_change_cycle_id_changed_at",
                table: "spark_cycle_change",
                columns: new[] { "cycle_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_daily_snapshot_cycle_id_date",
                table: "spark_daily_snapshot",
                columns: new[] { "cycle_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spark_grant_cycle_id",
                table: "spark_grant",
                column: "cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_spark_grant_queue_request_id",
                table: "spark_grant",
                column: "queue_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spark_grant_user_id",
                table: "spark_grant",
                column: "user_id",
                unique: true,
                filter: "kind = 0");

            migrationBuilder.CreateIndex(
                name: "IX_spark_grant_user_id_granted_at",
                table: "spark_grant",
                columns: new[] { "user_id", "granted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_queue_request_entry_order",
                table: "spark_queue_request",
                column: "entry_order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_spark_queue_request_state_decided_at",
                table: "spark_queue_request",
                columns: new[] { "state", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_queue_request_state_kind_entry_order",
                table: "spark_queue_request",
                columns: new[] { "state", "kind", "entry_order" });

            migrationBuilder.CreateIndex(
                name: "IX_spark_queue_request_user_id",
                table: "spark_queue_request",
                column: "user_id",
                unique: true,
                filter: "state IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spark_cycle_change");

            migrationBuilder.DropTable(
                name: "spark_daily_snapshot");

            migrationBuilder.DropTable(
                name: "spark_grant");

            migrationBuilder.DropTable(
                name: "spark_settings");

            migrationBuilder.DropTable(
                name: "spark_cycle");

            migrationBuilder.DropTable(
                name: "spark_queue_request");
        }
    }
}
