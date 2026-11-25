namespace Library.Application.Reports;

public sealed record DashboardResponse
{
    public int Books { get; init; }

    public int Copies { get; init; }

    public int Ebooks { get; init; }

    public int Members { get; init; }

    public int NewMembersThisMonth { get; init; }

    public int ActiveLoans { get; init; }

    public int ActiveEbookLoans { get; init; }

    public int OverdueLoans { get; init; }

    public int LoansToday { get; init; }

    public int ReturnsToday { get; init; }

    public int WaitingReservations { get; init; }

    public int ReadyReservations { get; init; }

    public int UnpaidFines { get; init; }

    public decimal UnpaidFinesAmount { get; init; }

    public IReadOnlyList<MostBorrowedBookResponse> TopBooksThisMonth { get; init; } = [];
}
