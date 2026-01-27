using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotificationTableUpdateFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notification_ActorId",
                table: "Notification",
                column: "ActorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Actor_Employee",
                table: "Notification",
                column: "ActorId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Recipient_Employee",
                table: "Notification",
                column: "RecipientId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Actor_Employee",
                table: "Notification");

            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Recipient_Employee",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Notification_ActorId",
                table: "Notification");
        }
    }
}
