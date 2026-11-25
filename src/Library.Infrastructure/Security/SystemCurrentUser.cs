using Library.Application.Abstractions;

namespace Library.Infrastructure.Security;

public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? MemberId => null;

    public string Name => "system";
}
