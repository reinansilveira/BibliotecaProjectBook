using Library.Application.Abstractions;

namespace Library.Api.Authorization;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? MemberId => _accessor.HttpContext?.User.GetMemberId();

    public string Name => _accessor.HttpContext?.User.Identity?.Name ?? "system";
}
