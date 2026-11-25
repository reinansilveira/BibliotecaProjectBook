using Library.Application.Common.Pagination;

namespace Library.Application.Reservations;

public sealed class ReservationListRequest : PageRequest
{
    public bool OpenOnly { get; init; } = true;
}
