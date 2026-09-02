using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerSupport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaPolicyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PriorityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstResponseMinutes = table.Column<int>(type: "int", nullable: false),
                    ResolutionMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaPolicies_TicketCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TicketCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlaPolicies_TicketPriorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "TicketPriorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SlaBreachLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreachType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaBreachLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaBreachLogs_SlaPolicies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalTable: "SlaPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlaBreachLogs_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketSlas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstResponseDue = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolutionDue = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstRespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstResponseBreached = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionBreached = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketSlas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketSlas_SlaPolicies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalTable: "SlaPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketSlas_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreachLogs_SlaPolicyId",
                table: "SlaBreachLogs",
                column: "SlaPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreachLogs_TenantId_TicketId",
                table: "SlaBreachLogs",
                columns: new[] { "TenantId", "TicketId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreachLogs_TicketId",
                table: "SlaBreachLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_CategoryId",
                table: "SlaPolicies",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_PriorityId",
                table: "SlaPolicies",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_TenantId_PriorityId_CategoryId",
                table: "SlaPolicies",
                columns: new[] { "TenantId", "PriorityId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlas_SlaPolicyId",
                table: "TicketSlas",
                column: "SlaPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlas_TenantId_FirstResponseBreached",
                table: "TicketSlas",
                columns: new[] { "TenantId", "FirstResponseBreached" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlas_TenantId_ResolutionBreached",
                table: "TicketSlas",
                columns: new[] { "TenantId", "ResolutionBreached" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlas_TicketId",
                table: "TicketSlas",
                column: "TicketId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlaBreachLogs");

            migrationBuilder.DropTable(
                name: "TicketSlas");

            migrationBuilder.DropTable(
                name: "SlaPolicies");
        }
    }
}
