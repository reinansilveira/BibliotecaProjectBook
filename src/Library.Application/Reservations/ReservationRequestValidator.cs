using FluentValidation;

namespace Library.Application.Reservations;

public sealed class ReservationRequestValidator : AbstractValidator<ReservationRequest>
{
    public ReservationRequestValidator()
    {
        RuleFor(request => request.BookId).NotEmpty();
        RuleFor(request => request.MemberId).NotEmpty();
        RuleFor(request => request.Format).IsInEnum();
    }
}
