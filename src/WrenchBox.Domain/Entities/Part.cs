using WrenchBox.Domain.Common;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Entities;

public class Part : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public int MinimumStock { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<StockMovement> _stockMovements = [];
    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    private Part() { }

    public static Part Create(string name, string sku, decimal unitPrice, int stockQuantity, int minimumStock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Part name is required.");

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Part SKU is required.");

        if (unitPrice < 0)
            throw new DomainException("Part unit price cannot be negative.");

        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        if (minimumStock < 0)
            throw new DomainException("Minimum stock cannot be negative.");

        return new Part
        {
            Name = name.Trim(),
            Sku = sku.Trim().ToUpperInvariant(),
            UnitPrice = unitPrice,
            StockQuantity = stockQuantity,
            MinimumStock = minimumStock
        };
    }

    public void Update(string name, decimal unitPrice, int minimumStock, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Part name is required.");

        if (unitPrice < 0)
            throw new DomainException("Part unit price cannot be negative.");

        if (minimumStock < 0)
            throw new DomainException("Minimum stock cannot be negative.");

        Name = name.Trim();
        UnitPrice = unitPrice;
        MinimumStock = minimumStock;
        IsActive = isActive;
        MarkUpdated();
    }

    public void AdjustStock(int quantity, string reason)
    {
        if (quantity == 0)
            throw new DomainException("Adjustment quantity cannot be zero.");

        var newQuantity = StockQuantity + quantity;
        if (newQuantity < 0)
            throw new DomainException("Insufficient stock for adjustment.");

        StockQuantity = newQuantity;
        _stockMovements.Add(StockMovement.Create(Id, StockMovementType.Adjustment, quantity, reason));
        MarkUpdated();
    }

    public void Deduct(int quantity, Guid? workOrderId, string reason)
    {
        if (quantity <= 0)
            throw new DomainException("Deduction quantity must be greater than zero.");

        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock for part '{Sku}'. Available: {StockQuantity}, requested: {quantity}.");

        StockQuantity -= quantity;
        _stockMovements.Add(StockMovement.Create(Id, StockMovementType.Deduction, -quantity, reason, workOrderId));
        MarkUpdated();
    }

    public bool HasSufficientStock(int quantity) => StockQuantity >= quantity;

    public bool IsBelowMinimumStock() => StockQuantity < MinimumStock;
}
