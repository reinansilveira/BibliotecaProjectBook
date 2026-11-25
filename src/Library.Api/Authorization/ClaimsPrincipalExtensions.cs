using System.Security.Claims;
using Library.Domain.Enums;
using Library.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetMemberId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimNames.MemberId), out var id) ? id : null;

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(Role.Admin));

    public static async Task<bool> CanAccessMemberAsync(this ControllerBase controller, IAuthorizationService authorizationService, Guid memberId)
    {
        var result = await authorizationService.AuthorizeAsync(controller.User, memberId, Policies.ResourceOwnerOrAdmin);

        return result.Succeeded;
    }
}
