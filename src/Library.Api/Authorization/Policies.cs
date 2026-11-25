namespace Library.Api.Authorization;

public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);

    public const string MemberOrAdmin = nameof(MemberOrAdmin);

    public const string ResourceOwnerOrAdmin = nameof(ResourceOwnerOrAdmin);
}
