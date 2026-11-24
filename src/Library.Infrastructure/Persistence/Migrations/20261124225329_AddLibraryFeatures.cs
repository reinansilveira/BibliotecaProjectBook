using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryFeatures : Migration
    {
        private const string BackfillCopies = """
            INSERT INTO dbo.BookCopies (Id, BookId, Barcode, AddedAt, WithdrawnAt)
            SELECT
                NEWID(),
                b.Id,
                CONCAT('CP-', UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 10))),
                b.CreatedAt,
                CASE WHEN b.IsDeleted = 1 THEN SYSUTCDATETIME() END
            FROM dbo.Books AS b;

            UPDATE l
            SET l.CopyId = c.Id
            FROM dbo.Loans AS l
            INNER JOIN dbo.BookCopies AS c ON c.BookId = l.BookId
            WHERE l.Format = 'Print';
            """;

        private const string CreateMonthlyOverdueReportProcedure = """
            CREATE OR ALTER PROCEDURE dbo.usp_GetMonthlyOverdueReport
                @Year INT,
                @Now  DATETIME2
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @Start DATETIME2 = DATEFROMPARTS(@Year, 1, 1);
                DECLARE @End   DATETIME2 = DATEFROMPARTS(@Year + 1, 1, 1);

                WITH Months AS (
                    SELECT Month
                    FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10), (11), (12)) AS M(Month)
                ),
                LoansOfYear AS (
                    SELECT
                        MONTH(l.DueDate) AS Month,
                        CASE WHEN COALESCE(l.ReturnedAt, @Now) > l.DueDate THEN 1 ELSE 0 END AS IsOverdue,
                        CASE WHEN COALESCE(l.ReturnedAt, @Now) > l.DueDate
                             THEN DATEDIFF(DAY, l.DueDate, COALESCE(l.ReturnedAt, @Now))
                        END AS DaysOverdue
                    FROM dbo.Loans AS l
                    WHERE l.Format = 'Print'
                      AND l.DueDate >= @Start
                      AND l.DueDate <  @End
                )
                SELECT
                    m.Month,
                    COUNT(l.Month) AS TotalLoans,
                    COALESCE(SUM(l.IsOverdue), 0) AS OverdueLoans,
                    CAST(CASE WHEN COUNT(l.Month) = 0 THEN 0
                              ELSE 100.0 * SUM(l.IsOverdue) / COUNT(l.Month)
                         END AS DECIMAL(5, 2)) AS OverduePercentage,
                    CAST(COALESCE(AVG(CAST(l.DaysOverdue AS DECIMAL(10, 2))), 0) AS DECIMAL(10, 2)) AS AverageDaysOverdue
                FROM Months AS m
                LEFT JOIN LoansOfYear AS l ON l.Month = m.Month
                GROUP BY m.Month
                ORDER BY m.Month;
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Loans_BookId_ReturnedAt",
                table: "Loans");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAt",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CopyId",
                table: "Loans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "Loans",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "Print");

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueNoticeSentAt",
                table: "Loans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadingLocation",
                table: "Loans",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReadingProgress",
                table: "Loans",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "Loans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewalCount",
                table: "Loans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Books",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Books",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                table: "Books",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EbookFileKey",
                table: "Books",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EbookLicenses",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Books",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<string>(
                name: "Isbn",
                table: "Books",
                type: "varchar(13)",
                unicode: false,
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublishedYear",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingsCount",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Action = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barcode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DaysOverdue = table.Column<int>(type: "int", nullable: false),
                    DailyRate = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fines_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fines_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OneTimeTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeTokens_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Format = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => new { x.MemberId, x.BookId });
                    table.ForeignKey(
                        name: "FK_WishlistItems_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(BackfillCopies);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BookId_Format_ReturnedAt",
                table: "Loans",
                columns: new[] { "BookId", "Format", "ReturnedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Loans_CopyId_Active",
                table: "Loans",
                column: "CopyId",
                unique: true,
                filter: "[CopyId] IS NOT NULL AND [ReturnedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Genre",
                table: "Books",
                column: "Genre",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true,
                filter: "[Isbn] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorId",
                table: "AuditEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityType_EntityId",
                table: "AuditEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAt",
                table: "AuditEntries",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_Barcode",
                table: "BookCopies",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BookId_WithdrawnAt",
                table: "BookCopies",
                columns: new[] { "BookId", "WithdrawnAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Fines_LoanId",
                table: "Fines",
                column: "LoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fines_MemberId_Status",
                table: "Fines",
                columns: new[] { "MemberId", "Status" })
                .Annotation("SqlServer:Include", new[] { "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MemberId_CreatedAt",
                table: "Notifications",
                columns: new[] { "MemberId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Unread",
                table: "Notifications",
                column: "MemberId",
                filter: "[ReadAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeTokens_MemberId_Purpose",
                table: "OneTimeTokens",
                columns: new[] { "MemberId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeTokens_TokenHash",
                table: "OneTimeTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId_Format_Status_CreatedAt",
                table: "Reservations",
                columns: new[] { "BookId", "Format", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ExpiresAt",
                table: "Reservations",
                column: "ExpiresAt",
                filter: "[Status] = 'Ready'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_MemberId_Status",
                table: "Reservations",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Reservations_Open",
                table: "Reservations",
                columns: new[] { "BookId", "MemberId", "Format" },
                unique: true,
                filter: "[Status] IN ('Waiting', 'Ready')");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookId_MemberId",
                table: "Reviews",
                columns: new[] { "BookId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookId_UpdatedAt",
                table: "Reviews",
                columns: new[] { "BookId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_MemberId",
                table: "Reviews",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_BookId",
                table: "WishlistItems",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_BookCopies_CopyId",
                table: "Loans",
                column: "CopyId",
                principalTable: "BookCopies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(CreateMonthlyOverdueReportProcedure);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(InitialCreate.CreateMonthlyOverdueReportProcedure);

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_BookCopies_CopyId",
                table: "Loans");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropTable(
                name: "Fines");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OneTimeTokens");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "StoredFiles");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_Loans_BookId_Format_ReturnedAt",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "UX_Loans_CopyId_Active",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Books_Genre",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CopyId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "OverdueNoticeSentAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ReadingLocation",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ReadingProgress",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "RenewalCount",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "EbookFileKey",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "EbookLicenses",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Isbn",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "PublishedYear",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "RatingsCount",
                table: "Books");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Books",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateTable(
                name: "BlacklistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlacklistEntries_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BookId_ReturnedAt",
                table: "Loans",
                columns: new[] { "BookId", "ReturnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistEntries_MemberId",
                table: "BlacklistEntries",
                column: "MemberId",
                unique: true);
        }
    }
}
