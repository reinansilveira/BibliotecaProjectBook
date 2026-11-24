using System.Data;
using Dapper;
using Library.Application.Reports;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Reports;

public sealed class ReportRepository : IReportRepository
{
    public const string MonthlyOverdueProcedure = "dbo.usp_GetMonthlyOverdueReport";

    private const string MostBorrowedBooksSql = """
        WITH LoansPerBook AS (
            SELECT l.BookId, COUNT(*) AS TotalLoans
            FROM dbo.Loans AS l
            WHERE (@From IS NULL OR l.LoanDate >= @From)
              AND (@ToExclusive IS NULL OR l.LoanDate < @ToExclusive)
            GROUP BY l.BookId
        )
        SELECT TOP (@Top)
            CAST(DENSE_RANK() OVER (ORDER BY c.TotalLoans DESC) AS INT) AS Rank,
            b.Id AS BookId,
            b.Title,
            b.Author,
            c.TotalLoans
        FROM LoansPerBook AS c
        INNER JOIN dbo.Books AS b ON b.Id = c.BookId
        ORDER BY c.TotalLoans DESC, b.Title;
        """;

    private readonly LibraryDbContext _context;

    public ReportRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(
        DateTime? from,
        DateTime? toExclusive,
        int top,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("From", from, DbType.DateTime2);
        parameters.Add("ToExclusive", toExclusive, DbType.DateTime2);
        parameters.Add("Top", top, DbType.Int32);

        var connection = _context.Database.GetDbConnection();
        var command = new CommandDefinition(MostBorrowedBooksSql, parameters, cancellationToken: cancellationToken);

        return (await connection.QueryAsync<MostBorrowedBookResponse>(command)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, DateTime now, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Year", year, DbType.Int32);
        parameters.Add("Now", now, DbType.DateTime2);

        var connection = _context.Database.GetDbConnection();
        var command = new CommandDefinition(
            MonthlyOverdueProcedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return (await connection.QueryAsync<MonthlyOverdueResponse>(command)).ToList();
    }
}
