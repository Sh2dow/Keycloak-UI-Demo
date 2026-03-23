using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitTasksDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if tasks table exists before creating it
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'tasks') THEN
                        CREATE TABLE tasks (
                            id uuid NOT NULL,
                            user_id uuid NOT NULL,
                            title character varying(200) NOT NULL,
                            description character varying(2000),
                            status character varying(32) NOT NULL,
                            priority character varying(32) NOT NULL,
                            created_at_utc timestamp with time zone NOT NULL,
                            updated_at_utc timestamp with time zone,
                            CONSTRAINT pk_tasks PRIMARY KEY (id)
                        );
                        CREATE INDEX ix_tasks_user_id ON tasks (user_id);
                    END IF;
                END $$;");

            // Check if task_comments table exists before creating it
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'task_comments') THEN
                        CREATE TABLE task_comments (
                            id uuid NOT NULL,
                            task_id uuid NOT NULL,
                            author_id uuid NOT NULL,
                            content character varying(1000) NOT NULL,
                            created_at_utc timestamp with time zone NOT NULL,
                            CONSTRAINT pk_task_comments PRIMARY KEY (id)
                        );
                        CREATE INDEX ix_task_comments_author_id ON task_comments (author_id);
                        CREATE INDEX ix_task_comments_task_id ON task_comments (task_id);
                        ALTER TABLE task_comments ADD CONSTRAINT fk_task_comments_tasks_task_id FOREIGN KEY (task_id) REFERENCES tasks (id) ON DELETE CASCADE;
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_comments");

            migrationBuilder.DropTable(
                name: "tasks");
        }
    }
}
