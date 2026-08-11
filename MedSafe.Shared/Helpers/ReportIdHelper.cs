namespace MedSafe.Shared;

public static class ReportIdHelper
{
    public static string Generate() =>
        $"RPT-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(100000, 999999)}";
}
