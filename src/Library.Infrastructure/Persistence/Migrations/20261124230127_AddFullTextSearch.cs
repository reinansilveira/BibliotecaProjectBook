using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        private const string CreateFullTextIndex = """
            IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1 AND DB_NAME() NOT IN ('master', 'tempdb', 'model', 'msdb')
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'LibraryCatalog')
                    EXEC('CREATE FULLTEXT CATALOG LibraryCatalog');

                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.Books'))
                    EXEC('CREATE FULLTEXT INDEX ON dbo.Books (Title, Author, Description) KEY INDEX PK_Books ON LibraryCatalog WITH CHANGE_TRACKING AUTO');
            END
            """;

        private const string DropFullTextIndex = """
            IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.Books'))
                EXEC('DROP FULLTEXT INDEX ON dbo.Books');

            IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'LibraryCatalog')
                EXEC('DROP FULLTEXT CATALOG LibraryCatalog');
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateFullTextIndex, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropFullTextIndex, suppressTransaction: true);
        }
    }
}
