using WrenchBox.Domain.Enums;

namespace WrenchBox.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PartId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid? WorkOrderId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private StockMovement() { }

    public static StockMovement Create(Guid partId, StockMovementType type, int quantity, string reason, Guid? workOrderId = null)
    {
        return new StockMovement
        {
            PartId = partId,
            Type = type,
            Quantity = quantity,
            Reason = reason,
            WorkOrderId = workOrderId
        };
    }
}
