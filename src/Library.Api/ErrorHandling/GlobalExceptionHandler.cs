using FluentValidation;
using Library.Application.Common.Exceptions;
using Library.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            UnauthorizedException => Create(StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            NotFoundException => Create(StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => Create(StatusCodes.Status409Conflict, "Conflict", exception.Message),
            DomainException => Create(StatusCodes.Status422UnprocessableEntity, "Business rule violation", exception.Message),
            _ => Create(StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.")
        };

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled error while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static ProblemDetails Create(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more fields are invalid."
        };
    }
}
