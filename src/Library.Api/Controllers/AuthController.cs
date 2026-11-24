using Library.Api.Authorization;
using Library.Api.RateLimiting;
using Library.Application.Auth;
using Library.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[EnableRateLimiting(RateLimitPolicies.Authentication)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMemberService _memberService;

    public AuthController(IAuthService authService, IMemberService memberService)
    {
        _authService = authService;
        _memberService = memberService;
    }

    /// <summary>Creates an account with the Member role. The password is stored as a PBKDF2 hash.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> Register(RegisterMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await _memberService.RegisterAsync(request, cancellationToken);

        return CreatedAtAction(nameof(MembersController.GetById), "Members", new { id = member.Id }, member);
    }

    /// <summary>Signs in and returns an access token (15 min) and a refresh token (7 days).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    /// <summary>Exchanges a refresh token for a new token pair. Each refresh token can be used only once; reusing one revokes every session of the member.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.RefreshAsync(request, cancellationToken));
    }

    /// <summary>Ends the session by revoking the refresh token. The current access token expires on its own within 15 minutes.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Returns the signed-in member.</summary>
    [HttpGet("me")]
    [DisableRateLimiting]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MemberResponse>> Me(CancellationToken cancellationToken)
    {
        var memberId = User.GetMemberId();

        if (memberId is null)
        {
            return Unauthorized();
        }

        return Ok(await _memberService.GetByIdAsync(memberId.Value, cancellationToken));
    }
}
