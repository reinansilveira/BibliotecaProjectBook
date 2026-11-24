using Library.Api.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Library.Api.Realtime;

public sealed class MemberIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.GetMemberId()?.ToString();
}
