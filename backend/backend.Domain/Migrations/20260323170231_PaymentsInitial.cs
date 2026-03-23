using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Domain.Migrations.Payments
{
    /// <inheritdoc />
    public partial class PaymentsInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if payment_event_records table exists before creating it
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'payment_event_records') THEN
                        CREATE TABLE payment_event_records (
                            id uuid NOT NULL,
                            payment_id uuid NOT NULL,
                            order_id uuid NOT NULL,
                            event_type character varying(32) NOT NULL,
                            amount numeric(18,2),
                            currency character varying(3),
                            occurred_at_utc timestamp with time zone NOT NULL,
                            sequence_number integer NOT NULL,
                            payload text,
                            created_at_utc timestamp with time zone NOT NULL,
                            CONSTRAINT pk_payment_event_records PRIMARY KEY (id)
                        );
                        CREATE INDEX ix_payment_event_records_order_id ON payment_event_records (order_id);
                        CREATE INDEX ix_payment_event_records_payment_id ON payment_event_records (payment_id);
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_event_records");
        }
    }
}
