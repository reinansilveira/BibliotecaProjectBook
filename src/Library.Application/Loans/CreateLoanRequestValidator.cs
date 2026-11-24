using FluentValidation;

namespace Library.Application.Loans;

public sealed class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(loan => loan.BookId).NotEmpty();
        RuleFor(loan => loan.MemberId).NotEmpty();
    }
}
