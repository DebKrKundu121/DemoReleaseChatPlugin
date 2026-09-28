using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoReleaseChatPlugin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Audit");

            migrationBuilder.EnsureSchema(
                name: "History");

            migrationBuilder.EnsureSchema(
                name: "Releases");

            migrationBuilder.EnsureSchema(
                name: "Authorization");

            migrationBuilder.CreateTable(
                name: "DomainEvents",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RecordedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ActionedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEvents", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "HistoryRecords",
                schema: "History",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HappenedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    HappenedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryRecords", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Authorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "Authorization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseNotices",
                schema: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetEnvironment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LeadDeveloper = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeployedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RolledBackOn = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RollbackReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RollbackOfReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RiskNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseNotices_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Releases",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseNotices_ReleaseNotices_RollbackOfReleaseId",
                        column: x => x.RollbackOfReleaseId,
                        principalSchema: "Releases",
                        principalTable: "ReleaseNotices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseNotices_ReleaseNotices_ScheduledReleaseId",
                        column: x => x.ScheduledReleaseId,
                        principalSchema: "Releases",
                        principalTable: "ReleaseNotices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StakeholderGroups",
                schema: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakeholderGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StakeholderGroups_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Releases",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "Authorization",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RolesId",
                        column: x => x.RolesId,
                        principalSchema: "Authorization",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UsersId",
                        column: x => x.UsersId,
                        principalSchema: "Authorization",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stakeholders",
                schema: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stakeholders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stakeholders_StakeholderGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Releases",
                        principalTable: "StakeholderGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_Name",
                schema: "Audit",
                table: "DomainEvents",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_OccurredOn",
                schema: "Audit",
                table: "DomainEvents",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryRecords_CorrelationId",
                schema: "History",
                table: "HistoryRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryRecords_HappenedOn",
                schema: "History",
                table: "HistoryRecords",
                column: "HappenedOn");

            migrationBuilder.CreateIndex(
                name: "UX_Projects_Name",
                schema: "Releases",
                table: "Projects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotices_ProjectId_CreatedOn",
                schema: "Releases",
                table: "ReleaseNotices",
                columns: new[] { "ProjectId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotices_RollbackOfReleaseId",
                schema: "Releases",
                table: "ReleaseNotices",
                column: "RollbackOfReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotices_ScheduledFor",
                schema: "Releases",
                table: "ReleaseNotices",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotices_ScheduledReleaseId",
                schema: "Releases",
                table: "ReleaseNotices",
                column: "ScheduledReleaseId");

            migrationBuilder.CreateIndex(
                name: "UX_ReleaseNotices_ProjectId_Kind_Version",
                schema: "Releases",
                table: "ReleaseNotices",
                columns: new[] { "ProjectId", "Kind", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Roles_Name",
                schema: "Authorization",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderGroups_ProjectId",
                schema: "Releases",
                table: "StakeholderGroups",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "UX_StakeholderGroups_ProjectId_Name",
                schema: "Releases",
                table: "StakeholderGroups",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_GroupId",
                schema: "Releases",
                table: "Stakeholders",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_ProjectId",
                schema: "Releases",
                table: "Stakeholders",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "UX_Stakeholders_ProjectId_Email",
                schema: "Releases",
                table: "Stakeholders",
                columns: new[] { "ProjectId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UsersId",
                schema: "Authorization",
                table: "UserRoles",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "UX_Users_ExternalId",
                schema: "Authorization",
                table: "Users",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainEvents",
                schema: "Audit");

            migrationBuilder.DropTable(
                name: "HistoryRecords",
                schema: "History");

            migrationBuilder.DropTable(
                name: "ReleaseNotices",
                schema: "Releases");

            migrationBuilder.DropTable(
                name: "Stakeholders",
                schema: "Releases");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "Authorization");

            migrationBuilder.DropTable(
                name: "StakeholderGroups",
                schema: "Releases");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Authorization");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Authorization");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "Releases");
        }
    }
}
