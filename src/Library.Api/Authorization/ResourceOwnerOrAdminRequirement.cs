using Microsoft.AspNetCore.Authorization;

namespace Library.Api.Authorization;

public sealed class ResourceOwnerOrAdminRequirement : IAuthorizationRequirement
{
}
