namespace WrenchBox.Application.Interfaces;

public interface IOperationalMetrics
{
    void WorkOrderOpened();
    void RecordPhaseDuration(string phase, double seconds);
    void IntegrationError(string integration);
}

public sealed class NullOperationalMetrics : IOperationalMetrics
{
    public static readonly NullOperationalMetrics Instance = new();

    public void WorkOrderOpened() { }

    public void RecordPhaseDuration(string phase, double seconds) { }

    public void IntegrationError(string integration) { }
}

public static class OperationalMetricsExtensions
{
    public static IOperationalMetrics OrNull(this IOperationalMetrics? metrics) =>
        metrics ?? NullOperationalMetrics.Instance;
}
