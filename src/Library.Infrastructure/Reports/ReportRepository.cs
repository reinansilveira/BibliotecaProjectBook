using System.Data;
using System.Globalization;
using Dapper;
using Library.Application.Reports;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Reports;

public sealed class ReportRepository : IReportRepository
{
    public const string MonthlyOverdueProcedure = "dbo.usp_GetMonthlyOverdueReport";

    private const int TopBooksOnDashboard = 5;
    private const int TopGenres = 5;

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

    private const string AlsoReadSql = """
        WITH Readers AS (
            SELECT DISTINCT l.MemberId
            FROM dbo.Loans AS l
            WHERE l.BookId = @BookId
        )
        SELECT TOP (@Top) l.BookId
        FROM dbo.Loans AS l
        INNER JOIN Readers AS r ON r.MemberId = l.MemberId
        INNER JOIN dbo.Books AS b ON b.Id = l.BookId
        WHERE l.BookId <> @BookId
          AND b.IsDeleted = 0
        GROUP BY l.BookId
        ORDER BY COUNT(DISTINCT l.MemberId) DESC, MAX(b.AverageRating) DESC, MAX(b.Title);
        """;

    private const string DashboardSql = """
        SELECT
            (SELECT COUNT(*) FROM dbo.Books WHERE IsDeleted = 0) AS Books,
            (SELECT COUNT(*) FROM dbo.BookCopies AS c INNER JOIN dbo.Books AS b ON b.Id = c.BookId WHERE c.WithdrawnAt IS NULL AND b.IsDeleted = 0) AS Copies,
            (SELECT COUNT(*) FROM dbo.Books WHERE IsDeleted = 0 AND EbookFileKey IS NOT NULL AND EbookLicenses > 0) AS Ebooks,
            (SELECT COUNT(*) FROM dbo.Members WHERE IsDeleted = 0) AS Members,
            (SELECT COUNT(*) FROM dbo.Members WHERE IsDeleted = 0 AND CreatedAt >= @MonthStart) AS NewMembersThisMonth,
            (SELECT COUNT(*) FROM dbo.Loans WHERE ReturnedAt IS NULL AND Format = 'Print') AS ActiveLoans,
            (SELECT COUNT(*) FROM dbo.Loans WHERE ReturnedAt IS NULL AND Format = 'Ebook' AND DueDate > @Now) AS ActiveEbookLoans,
            (SELECT COUNT(*) FROM dbo.Loans WHERE ReturnedAt IS NULL AND Format = 'Print' AND DueDate < @Now) AS OverdueLoans,
            (SELECT COUNT(*) FROM dbo.Loans WHERE LoanDate >= @Today AND LoanDate < @Tomorrow) AS LoansToday,
            (SELECT COUNT(*) FROM dbo.Loans WHERE ReturnedAt >= @Today AND ReturnedAt < @Tomorrow) AS ReturnsToday,
            (SELECT COUNT(*) FROM dbo.Reservations WHERE Status = 'Waiting') AS WaitingReservations,
            (SELECT COUNT(*) FROM dbo.Reservations WHERE Status = 'Ready') AS ReadyReservations,
            (SELECT COUNT(*) FROM dbo.Fines WHERE Status = 'Unpaid') AS UnpaidFines,
            (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Fines WHERE Status = 'Unpaid') AS UnpaidFinesAmount;
        """;

    private const string ReadingStatsSql = """
        SELECT
            COUNT(*) AS TotalLoans,
            COUNT(DISTINCT CASE WHEN l.ReturnedAt IS NOT NULL THEN l.BookId END) AS BooksRead,
            COUNT(DISTINCT CASE WHEN l.Format = 'Ebook' THEN l.BookId END) AS EbooksRead,
            COALESCE(SUM(CASE WHEN l.ReturnedAt IS NULL AND (l.Format = 'Print' OR l.DueDate > @Now) THEN 1 ELSE 0 END), 0) AS ActiveLoans,
            COUNT(DISTINCT CASE WHEN l.LoanDate >= @YearStart THEN l.BookId END) AS BooksThisYear
        FROM dbo.Loans AS l
        WHERE l.MemberId = @MemberId;

        SELECT
            COUNT(*) AS ReviewsWritten,
            CAST(COALESCE(AVG(CAST(r.Rating AS DECIMAL(5, 2))), 0) AS DECIMAL(5, 2)) AS AverageRatingGiven
        FROM dbo.Reviews AS r
        WHERE r.MemberId = @MemberId;

        SELECT TOP (@TopGenres) b.Genre, COUNT(*) AS Loans
        FROM dbo.Loans AS l
        INNER JOIN dbo.Books AS b ON b.Id = l.BookId
        WHERE l.MemberId = @MemberId
        GROUP BY b.Genre
        ORDER BY COUNT(*) DESC, b.Genre;

        SELECT FORMAT(l.LoanDate, 'yyyy-MM') AS Month, COUNT(*) AS Loans
        FROM dbo.Loans AS l
        WHERE l.MemberId = @MemberId AND l.LoanDate >= @FirstMonth
        GROUP BY FORMAT(l.LoanDate, 'yyyy-MM');
        """;

    private const string LoansSql = """
        SELECT
            l.Id AS LoanId,
            b.Title AS BookTitle,
            b.Author AS BookAuthor,
            m.Name AS MemberName,
            m.Email AS MemberEmail,
            l.Format,
            l.LoanDate,
            l.DueDate,
            l.ReturnedAt,
            l.RenewalCount
        FROM dbo.Loans AS l
        INNER JOIN dbo.Books AS b ON b.Id = l.BookId
        INNER JOIN dbo.Members AS m ON m.Id = l.MemberId
        WHERE (@From IS NULL OR l.LoanDate >= @From)
          AND (@ToExclusive IS NULL OR l.LoanDate < @ToExclusive)
        ORDER BY l.LoanDate DESC;
        """;

    private readonly LibraryDbContext _context;

    public ReportRepository(LibraryDbContext context)
    {
        _context = context;
    }

    private IDbConnection Connection => _context.Database.GetDbConnection();

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

        var command = new CommandDefinition(MostBorrowedBooksSql, parameters, cancellationToken: cancellationToken);

        return (await Connection.QueryAsync<MostBorrowedBookResponse>(command)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, DateTime now, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Year", year, DbType.Int32);
        parameters.Add("Now", now, DbType.DateTime2);

        var command = new CommandDefinition(
            MonthlyOverdueProcedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return (await Connection.QueryAsync<MonthlyOverdueResponse>(command)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetAlsoReadBookIdsAsync(Guid bookId, int top, CancellationToken cancellationToken = default)
    {
        var command = new CommandDefinition(AlsoReadSql, new { BookId = bookId, Top = top }, cancellationToken: cancellationToken);

        return (await Connection.QueryAsync<Guid>(command)).ToList();
    }

    public async Task<DashboardResponse> GetDashboardAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var parameters = new { Now = now, Today = today, Tomorrow = today.AddDays(1), MonthStart = monthStart };

        var dashboard = await Connection.QuerySingleAsync<DashboardResponse>(new CommandDefinition(DashboardSql, parameters, cancellationToken: cancellationToken));
        var topBooks = await GetMostBorrowedBooksAsync(monthStart, null, TopBooksOnDashboard, cancellationToken);

        return dashboard with { TopBooksThisMonth = topBooks };
    }

    public async Task<ReadingStatsResponse> GetReadingStatsAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default)
    {
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var parameters = new
        {
            MemberId = memberId,
            Now = now,
            YearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FirstMonth = firstMonth,
            TopGenres
        };

        await using var results = await ((System.Data.Common.DbConnection)Connection).QueryMultipleAsync(
            new CommandDefinition(ReadingStatsSql, parameters, cancellationToken: cancellationToken));

        var totals = await results.ReadSingleAsync<ReadingStatsResponse>();
        var reviews = await results.ReadSingleAsync<ReviewTotals>();
        var genres = (await results.ReadAsync<GenreCount>()).ToList();
        var months = (await results.ReadAsync<MonthCount>()).ToDictionary(month => month.Month, month => month.Loans);

        var lastTwelveMonths = Enumerable.Range(0, 12)
            .Select(offset => firstMonth.AddMonths(offset).ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .Select(month => new MonthCount { Month = month, Loans = months.GetValueOrDefault(month) })
            .ToList();

        return totals with
        {
            ReviewsWritten = reviews.ReviewsWritten,
            AverageRatingGiven = reviews.AverageRatingGiven,
            FavoriteGenre = genres.FirstOrDefault()?.Genre,
            TopGenres = genres,
            LastTwelveMonths = lastTwelveMonths
        };
    }

    public async Task<IReadOnlyList<LoanExportRow>> GetLoansAsync(DateTime? from, DateTime? toExclusive, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("From", from, DbType.DateTime2);
        parameters.Add("ToExclusive", toExclusive, DbType.DateTime2);

        return (await Connection.QueryAsync<LoanExportRow>(new CommandDefinition(LoansSql, parameters, cancellationToken: cancellationToken))).ToList();
    }

    private sealed record ReviewTotals
    {
        public int ReviewsWritten { get; init; }

        public decimal AverageRatingGiven { get; init; }
    }
}
