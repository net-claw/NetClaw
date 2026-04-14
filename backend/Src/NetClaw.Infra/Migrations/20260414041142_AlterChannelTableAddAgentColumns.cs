using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetClaw.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AlterChannelTableAddAgentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "agent_id",
                table: "channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "agent_team_id",
                table: "channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_channels_agent_id",
                table: "channels",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_channels_agent_team_id",
                table: "channels",
                column: "agent_team_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_channels_agent_id",
                table: "channels");

            migrationBuilder.DropIndex(
                name: "IX_channels_agent_team_id",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "agent_id",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "agent_team_id",
                table: "channels");
        }
    }
}
