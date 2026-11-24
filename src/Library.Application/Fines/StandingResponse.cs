namespace Library.Application.Fines;

public sealed record StandingResponse(
    bool CanBorrow,
    int ActiveLoans,
    int MaxActiveLoans,
    int OverdueLoans,
    decimal UnpaidFines,
    string Currency,
    IReadOnlyList<string> Reasons);
