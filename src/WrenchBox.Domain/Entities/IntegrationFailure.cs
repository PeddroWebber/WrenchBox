using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Entities;

public class IntegrationFailure
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Integration { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public string Detail { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    private IntegrationFailure() { }

    public static IntegrationFailure Create(string integration, string? reference, string detail)
    {
        if (string.IsNullOrWhiteSpace(integration))
            throw new DomainException("Integration name is required.");

        return new IntegrationFailure
        {
            Integration = integration.Trim(),
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Detail = detail?.Trim() ?? string.Empty
        };
    }
}
