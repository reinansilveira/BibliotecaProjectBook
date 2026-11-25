using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.RateLimiting;
using Library.Api.Versioning;
using Library.Application.Account;
using Library.Application.Auth;
using Library.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/auth")]
[Produces("application/json")]
[EnableRateLimiting(RateLimitPolicies.Authentication)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMemberService _memberService;
    private readonly IAccountService _accountService;

    public AuthController(IAuthService authService, IMemberService memberService, IAccountService accountService)
    {
        _authService = authService;
        _memberService = memberService;
        _accountService = accountService;
    }

    /// <summary>Creates an account with the Member role and sends an email confirmation link. The password is stored as a PBKDF2 hash.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> Register(RegisterMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await _memberService.RegisterAsync(request, cancellationToken);
        await _accountService.SendEmailConfirmationAsync(member.Id, cancellationToken);

        return CreatedAtAction(nameof(MembersController.GetById), "Members", new { id = member.Id, version = ApiRoutes.Version1 }, member);
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

    /// <summary>Confirms the email address with the single-use token sent by email.</summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ConfirmEmailAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Sends a new confirmation link to the signed-in member.</summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendConfirmation(CancellationToken cancellationToken)
    {
        var memberId = User.GetMemberId();

        if (memberId is null)
        {
            return Unauthorized();
        }

        await _accountService.SendEmailConfirmationAsync(memberId.Value, cancellationToken);

        return Accepted();
    }

    /// <summary>Sends a password reset link valid for one hour. Always answers 202, so it does not reveal which emails exist.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ForgotPasswordAsync(request, cancellationToken);

        return Accepted();
    }

    /// <summary>Sets a new password with the single-use reset token and signs the member out of every session.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ResetPasswordAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Changes the password of the signed-in member and signs out every session.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var memberId = User.GetMemberId();

        if (memberId is null)
        {
            return Unauthorized();
        }

        await _accountService.ChangePasswordAsync(memberId.Value, request, cancellationToken);

        return NoContent();
    }
}
