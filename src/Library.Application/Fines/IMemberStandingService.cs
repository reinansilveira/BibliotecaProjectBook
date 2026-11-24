namespace Library.Application.Fines;

public interface IMemberStandingService
{
    Task<StandingResponse> GetAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task EnsureCanBorrowAsync(Guid memberId, CancellationToken cancellationToken = default);
}
