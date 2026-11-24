using Library.Api.Authorization;
using Library.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Ranking of the most borrowed books in a period (dates as YYYY-MM-DD, both inclusive). Hand-written SQL with a CTE and DENSE_RANK, run with Dapper.</summary>
    [HttpGet("most-borrowed-books")]
    [ProducesResponseType<IReadOnlyList<MostBorrowedBookResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MostBorrowedBookResponse>>> MostBorrowedBooks(
        [FromQuery] MostBorrowedBooksRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetMostBorrowedBooksAsync(request, cancellationToken));
    }

    /// <summary>Loans and overdue returns for each month of the year, by due date. Runs the dbo.usp_GetMonthlyOverdueReport stored procedure.</summary>
    [HttpGet("overdue-by-month")]
    [ProducesResponseType<IReadOnlyList<MonthlyOverdueResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MonthlyOverdueResponse>>> OverdueByMonth([FromQuery] int year, CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetMonthlyOverdueAsync(year, cancellationToken));
    }
}
