using Asp.Versioning;
using Library.Api.Versioning;
using Library.Application.Loans.Reading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/ebooks")]
public sealed class EbooksController : ControllerBase
{
    private readonly IEbookReadingService _readingService;

    public EbooksController(IEbookReadingService readingService)
    {
        _readingService = readingService;
    }

    /// <summary>Streams the e-book file. Needs no token: the HMAC signature and the expiry in the link prove access, and the loan must still be active.</summary>
    [HttpGet("{loanId:guid}/content")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Content(Guid loanId, [FromQuery] long expires, [FromQuery] string signature, CancellationToken cancellationToken)
    {
        var file = await _readingService.OpenAsync(loanId, expires, signature ?? string.Empty, cancellationToken);

        Response.Headers.CacheControl = "private, no-store";

        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }
}
