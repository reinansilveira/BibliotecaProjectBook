using FluentValidation;
using Library.Application.Common.Pagination;

namespace Library.Application.Audit;

public sealed class AuditListRequestValidator : AbstractValidator<AuditListRequest>
{
    public AuditListRequestValidator()
    {
        Include(new PageRequestValidator());
        RuleFor(request => request.EntityType).MaximumLength(50);
        RuleFor(request => request.EntityId).MaximumLength(100);
        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .When(request => request.From.HasValue && request.To.HasValue)
            .WithMessage("The end date must be on or after the start date.");
    }
}
