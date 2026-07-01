using WrenchBox.Domain.Common;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Entities;

public class Service : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Service() { }

    public static Service Create(string name, string description, decimal unitPrice, int estimatedDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name is required.");

        if (unitPrice < 0)
            throw new DomainException("Service unit price cannot be negative.");

        if (estimatedDurationMinutes <= 0)
            throw new DomainException("Estimated duration must be greater than zero.");

        return new Service
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            UnitPrice = unitPrice,
            EstimatedDurationMinutes = estimatedDurationMinutes
        };
    }

    public void Update(string name, string description, decimal unitPrice, int estimatedDurationMinutes, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name is required.");

        if (unitPrice < 0)
            throw new DomainException("Service unit price cannot be negative.");

        if (estimatedDurationMinutes <= 0)
            throw new DomainException("Estimated duration must be greater than zero.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UnitPrice = unitPrice;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        IsActive = isActive;
        MarkUpdated();
    }
}
