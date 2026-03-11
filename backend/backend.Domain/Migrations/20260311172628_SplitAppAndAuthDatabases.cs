using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SplitAppAndAuthDatabases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_app_users_user_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_task_comments_app_users_author_id",
                table: "task_comments");

            migrationBuilder.DropForeignKey(
                name: "fk_tasks_app_users_user_id",
                table: "tasks");

            migrationBuilder.DropTable(
                name: "app_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_users_subject",
                table: "app_users",
                column: "subject",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_app_users_user_id",
                table: "orders",
                column: "user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_task_comments_app_users_author_id",
                table: "task_comments",
                column: "author_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tasks_app_users_user_id",
                table: "tasks",
                column: "user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
