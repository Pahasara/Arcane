using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arcane.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TitleEncrypted = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TitleNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentEncrypted = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Mood = table.Column<int>(type: "INTEGER", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaultProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Salt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Argon2Iterations = table.Column<int>(type: "INTEGER", nullable: false),
                    Argon2MemoryKiB = table.Column<int>(type: "INTEGER", nullable: false),
                    Argon2Parallelism = table.Column<int>(type: "INTEGER", nullable: false),
                    VerificationCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VerificationNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    LastUnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AutoLockMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ThemePreference = table.Column<string>(type: "TEXT", nullable: false),
                    EditorFontSize = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileNameEncrypted = table.Column<byte[]>(type: "BLOB", nullable: false),
                    FileNameNonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 127, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DataEncrypted = table.Column<byte[]>(type: "BLOB", nullable: true),
                    DataNonce = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IsStoredExternally = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalPathEncrypted = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ExternalPathNonce = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntryTags",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryTags", x => new { x.EntryId, x.TagId });
                    table.ForeignKey(
                        name: "FK_EntryTags_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntryTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EntryId",
                table: "Attachments",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_EntryTags_TagId",
                table: "EntryTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "EntryTags");

            migrationBuilder.DropTable(
                name: "VaultProfiles");

            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
