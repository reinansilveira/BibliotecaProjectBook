namespace Library.Application.Fines;

public sealed class FineSettings
{
    public const string SectionName = "Fines";

    public decimal DailyRate { get; set; } = 0.50m;

    public string Currency { get; set; } = "USD";
}
