using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DexGameBacklog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "Objectives",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Games",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CoverUrl",
                table: "Games",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BackgroundUrl",
                table: "Games",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_GameId_Label",
                table: "Objectives",
                columns: new[] { "GameId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameStatusHistories_ChangedBy",
                table: "GameStatusHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Games_UserId_Title_Platform",
                table: "Games",
                columns: new[] { "UserId", "Title", "Platform" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Users_UserId",
                table: "Games",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameStatusHistories_Users_ChangedBy",
                table: "GameStatusHistories",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Objectives_Games_GameId",
                table: "Objectives",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Users_UserId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_GameStatusHistories_Users_ChangedBy",
                table: "GameStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Objectives_Games_GameId",
                table: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_Objectives_GameId_Label",
                table: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_GameStatusHistories_ChangedBy",
                table: "GameStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Games_UserId_Title_Platform",
                table: "Games");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "Objectives",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Games",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CoverUrl",
                table: "Games",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BackgroundUrl",
                table: "Games",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
