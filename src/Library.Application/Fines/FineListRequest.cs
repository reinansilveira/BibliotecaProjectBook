using Library.Application.Common.Pagination;
using Library.Domain.Enums;

namespace Library.Application.Fines;

public sealed class FineListRequest : PageRequest
{
    public FineStatus? Status { get; init; }
}
