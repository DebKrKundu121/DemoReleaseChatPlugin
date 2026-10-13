using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoReleaseChatPlugin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProjectDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                schema: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcrProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NerProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatchConfidence = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: true),
                    MatchReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AddedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    AddedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Releases",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ProjectId_AddedOn",
                schema: "Releases",
                table: "ProjectDocuments",
                columns: new[] { "ProjectId", "AddedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDocuments",
                schema: "Releases");
        }
    }
}
