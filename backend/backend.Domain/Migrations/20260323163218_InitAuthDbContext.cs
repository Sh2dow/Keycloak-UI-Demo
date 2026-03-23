using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitAuthDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if app_users table exists before creating it
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'app_users') THEN
                        CREATE TABLE app_users (
                            id uuid NOT NULL,
                            subject character varying(64) NOT NULL,
                            username character varying(100) NOT NULL,
                            email character varying(200),
                            created_at_utc timestamp with time zone NOT NULL,
                            CONSTRAINT pk_app_users PRIMARY KEY (id)
                        );
                        CREATE INDEX ix_app_users_subject ON app_users (subject);
                    END IF;
                END $$;");

            // Check if outbox_messages table exists before creating it
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'outbox_messages') THEN
                        CREATE TABLE outbox_messages (
                            id uuid NOT NULL,
                            event_type text NOT NULL,
                            routing_key text NOT NULL,
                            payload text NOT NULL,
                            correlation_id text,
                            occurred_at_utc timestamp with time zone NOT NULL,
                            published_at_utc timestamp with time zone,
                            publish_attempts integer NOT NULL,
                            last_error text,
                            CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
                        );
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_users");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
