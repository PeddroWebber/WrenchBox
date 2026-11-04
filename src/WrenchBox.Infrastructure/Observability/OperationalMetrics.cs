using System.Diagnostics.Metrics;
using WrenchBox.Application.Interfaces;

namespace WrenchBox.Infrastructure.Observability;

public sealed class OperationalMetrics : IOperationalMetrics, IDisposable
{
    public const string MeterName = "WrenchBox";

    private readonly Meter _meter;
    private readonly Counter<long> _opened;
    private readonly Histogram<double> _phaseDuration;
    private readonly Counter<long> _integrationErrors;

    public OperationalMetrics()
    {
        _meter = new Meter(MeterName);
        _opened = _meter.CreateCounter<long>("wrenchbox.work_orders.opened");
        _phaseDuration = _meter.CreateHistogram<double>("wrenchbox.work_orders.phase_duration", unit: "s");
        _integrationErrors = _meter.CreateCounter<long>("wrenchbox.integrations.errors");
    }

    public void WorkOrderOpened() => _opened.Add(1);

    public void RecordPhaseDuration(string phase, double seconds) =>
        _phaseDuration.Record(seconds, new KeyValuePair<string, object?>("phase", phase));

    public void IntegrationError(string integration) =>
        _integrationErrors.Add(1, new KeyValuePair<string, object?>("integration", integration));

    public void Dispose() => _meter.Dispose();
}
