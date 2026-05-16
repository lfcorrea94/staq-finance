using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StaqFinance.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    NextRunOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringTransactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTransactionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedTransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTransactionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransactionRuns_RecurringTransactions_RecurringTra~",
                        column: x => x.RecurringTransactionId,
                        principalTable: "RecurringTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringTransactionRuns_Transactions_GeneratedTransactionId",
                        column: x => x.GeneratedTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactionRuns_GeneratedTransactionId",
                table: "RecurringTransactionRuns",
                column: "GeneratedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactionRuns_RecurringTransactionId_RunDate",
                table: "RecurringTransactionRuns",
                columns: new[] { "RecurringTransactionId", "RunDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactionRuns_TenantId",
                table: "RecurringTransactionRuns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_AccountId",
                table: "RecurringTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_CategoryId",
                table: "RecurringTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_NextRunOn",
                table: "RecurringTransactions",
                column: "NextRunOn");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_TenantId",
                table: "RecurringTransactions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringTransactionRuns");

            migrationBuilder.DropTable(
                name: "RecurringTransactions");
        }
    }
}
