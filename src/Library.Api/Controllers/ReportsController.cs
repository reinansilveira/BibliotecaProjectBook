using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Common.Csv;
using Library.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/reports")]
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

    /// <summary>Numbers of the day for the admin dashboard: loans, overdue, reservations, fines and the top books of the month.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetDashboardAsync(cancellationToken));
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

    /// <summary>The most borrowed books ranking as a CSV file.</summary>
    [HttpGet("most-borrowed-books/export")]
    [Produces(CsvFile.ContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportMostBorrowedBooks([FromQuery] MostBorrowedBooksRequest request, CancellationToken cancellationToken)
    {
        return ToFile(await _reportService.ExportMostBorrowedBooksAsync(request, cancellationToken));
    }

    /// <summary>Printed loans and overdue returns for each month of the year, by due date. Runs the dbo.usp_GetMonthlyOverdueReport stored procedure.</summary>
    [HttpGet("overdue-by-month")]
    [ProducesResponseType<IReadOnlyList<MonthlyOverdueResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MonthlyOverdueResponse>>> OverdueByMonth([FromQuery] int year, CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetMonthlyOverdueAsync(year, cancellationToken));
    }

    /// <summary>The monthly overdue report as a CSV file.</summary>
    [HttpGet("overdue-by-month/export")]
    [Produces(CsvFile.ContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportOverdueByMonth([FromQuery] int year, CancellationToken cancellationToken)
    {
        return ToFile(await _reportService.ExportMonthlyOverdueAsync(year, cancellationToken));
    }

    /// <summary>Every loan in a period as a CSV file, ready for Excel. Values that start like a formula are escaped.</summary>
    [HttpGet("loans/export")]
    [Produces(CsvFile.ContentType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportLoans([FromQuery] LoanExportRequest request, CancellationToken cancellationToken)
    {
        return ToFile(await _reportService.ExportLoansAsync(request, cancellationToken));
    }

    private FileContentResult ToFile(CsvFile file) => File(file.Content, CsvFile.ContentType, file.FileName);
}
