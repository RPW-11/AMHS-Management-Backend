using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MissionTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Mission",
                table: "Mission");

            migrationBuilder.RenameTable(
                name: "Mission",
                newName: "Missions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Missions",
                table: "Missions",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Missions",
                table: "Missions");

            migrationBuilder.RenameTable(
                name: "Missions",
                newName: "Mission");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Mission",
                table: "Mission",
                column: "Id");
        }
    }
}
