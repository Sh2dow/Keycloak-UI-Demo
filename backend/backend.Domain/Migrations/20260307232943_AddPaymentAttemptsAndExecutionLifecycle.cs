using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAttemptsAndExecutionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempt_number",
                table: "payment_event_records",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "execution_completed_at_utc",
                table: "order_saga_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "execution_failed_at_utc",
                table: "order_saga_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "execution_failure_reason",
                table: "order_saga_states",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "execution_started_at_utc",
                table: "order_saga_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_event_records_order_id_attempt_number_sequence_numb",
                table: "payment_event_records",
                columns: new[] { "order_id", "attempt_number", "sequence_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_event_records_order_id_attempt_number_sequence_numb",
                table: "payment_event_records");

            migrationBuilder.DropColumn(
                name: "attempt_number",
                table: "payment_event_records");

            migrationBuilder.DropColumn(
                name: "execution_completed_at_utc",
                table: "order_saga_states");

            migrationBuilder.DropColumn(
                name: "execution_failed_at_utc",
                table: "order_saga_states");

            migrationBuilder.DropColumn(
                name: "execution_failure_reason",
                table: "order_saga_states");

            migrationBuilder.DropColumn(
                name: "execution_started_at_utc",
                table: "order_saga_states");
        }
    }
}
