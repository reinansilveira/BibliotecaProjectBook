using Microsoft.AspNetCore.Authorization;

namespace Library.Api.Authorization;

public sealed class ResourceOwnerOrAdminHandler : AuthorizationHandler<ResourceOwnerOrAdminRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerOrAdminRequirement requirement,
        Guid memberId)
    {
        if (context.User.IsAdmin() || context.User.GetMemberId() == memberId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
