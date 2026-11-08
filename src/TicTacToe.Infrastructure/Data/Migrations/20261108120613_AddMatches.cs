using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpponentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: true),
                    Symbol = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    Moves = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_AspNetUsers_OpponentUserId",
                        column: x => x.OpponentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_OpponentUserId",
                table: "Matches",
                column: "OpponentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UserId_PlayedAtUtc",
                table: "Matches",
                columns: new[] { "UserId", "PlayedAtUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
