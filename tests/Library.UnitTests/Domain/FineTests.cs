using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class FineTests
{
    private static readonly DateTime Now = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Amount_is_days_times_the_daily_rate()
    {
        var fine = new Fine(Guid.NewGuid(), Guid.NewGuid(), 3, 0.50m, Now);

        Assert.Equal(1.50m, fine.Amount);
        Assert.Equal(FineStatus.Unpaid, fine.Status);
    }

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(2, 0)]
    public void Invalid_days_or_rate_are_rejected(int days, double rate)
    {
        Assert.Throws<DomainException>(() => new Fine(Guid.NewGuid(), Guid.NewGuid(), days, (decimal)rate, Now));
    }

    [Fact]
    public void Paying_keeps_the_reference_and_cannot_happen_twice()
    {
        var fine = new Fine(Guid.NewGuid(), Guid.NewGuid(), 2, 1m, Now);

        fine.Pay("SIM-123", Now);

        Assert.Equal(FineStatus.Paid, fine.Status);
        Assert.Equal("SIM-123", fine.PaymentReference);
        Assert.Throws<DomainException>(() => fine.Waive(Now));
    }

    [Fact]
    public void Waived_fine_is_settled()
    {
        var fine = new Fine(Guid.NewGuid(), Guid.NewGuid(), 2, 1m, Now);

        fine.Waive(Now);

        Assert.Equal(FineStatus.Waived, fine.Status);
        Assert.Equal(Now, fine.ResolvedAt);
    }
}
