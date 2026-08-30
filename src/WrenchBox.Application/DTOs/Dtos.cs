using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Application.DTOs;

public record CustomerDto(Guid Id, string Document, string Name, string Email, string Phone, DateTime CreatedAt);
public record VehicleDto(Guid Id, Guid CustomerId, string Plate, string Brand, string Model, int Year, DateTime CreatedAt);
public record ServiceDto(Guid Id, string Name, string Description, decimal UnitPrice, int EstimatedDurationMinutes, bool IsActive, DateTime CreatedAt);
public record PartDto(Guid Id, string Name, string Sku, decimal UnitPrice, int StockQuantity, int MinimumStock, bool IsActive, bool IsBelowMinimumStock, DateTime CreatedAt);

public record WorkOrderServiceItemDto(Guid ServiceId, string ServiceName, int Quantity, decimal UnitPrice, decimal TotalPrice);
public record WorkOrderPartItemDto(Guid PartId, string PartName, string PartSku, int Quantity, decimal UnitPrice, decimal TotalPrice);
public record WorkOrderStatusHistoryDto(WorkOrderStatus FromStatus, WorkOrderStatus ToStatus, DateTime ChangedAt, string? ChangedBy);

public record WorkOrderStatusDto(Guid Id, string OrderNumber, WorkOrderStatus Status, string StatusLabel);

public record WorkOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehiclePlate,
    WorkOrderStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    string? Notes,
    string? TrackingToken,
    IReadOnlyList<WorkOrderServiceItemDto> Services,
    IReadOnlyList<WorkOrderPartItemDto> Parts,
    IReadOnlyList<WorkOrderStatusHistoryDto> StatusHistory,
    DateTime CreatedAt);

public record TrackingWorkOrderDto(
    string OrderNumber,
    WorkOrderStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    IReadOnlyList<WorkOrderServiceItemDto> Services,
    IReadOnlyList<WorkOrderPartItemDto> Parts,
    IReadOnlyList<WorkOrderStatusHistoryDto> StatusHistory,
    DateTime CreatedAt);

public record SendBudgetResponseDto(Guid WorkOrderId, string TrackingToken, bool NotificationSent);
public record LoginResponseDto(string Token, DateTime ExpiresAt);
public record AverageExecutionTimeDto(double AverageMinutes, int CompletedOrdersCount);

public static class Mappers
{
    public static CustomerDto ToDto(this Customer c) =>
        new(c.Id, c.Document, c.Name, c.Email, c.Phone, c.CreatedAt);

    public static VehicleDto ToDto(this Vehicle v) =>
        new(v.Id, v.CustomerId, v.Plate, v.Brand, v.Model, v.Year, v.CreatedAt);

    public static ServiceDto ToDto(this Service s) =>
        new(s.Id, s.Name, s.Description, s.UnitPrice, s.EstimatedDurationMinutes, s.IsActive, s.CreatedAt);

    public static PartDto ToDto(this Part p) =>
        new(p.Id, p.Name, p.Sku, p.UnitPrice, p.StockQuantity, p.MinimumStock, p.IsActive, p.IsBelowMinimumStock(), p.CreatedAt);

    public static WorkOrderDto ToDto(this WorkOrder wo)
    {
        return new WorkOrderDto(
            wo.Id,
            wo.OrderNumber,
            wo.CustomerId,
            wo.Customer?.Name ?? string.Empty,
            wo.VehicleId,
            wo.Vehicle?.Plate ?? string.Empty,
            wo.Status,
            wo.Status.ToPortuguese(),
            wo.TotalAmount,
            wo.Notes,
            wo.TrackingToken,
            wo.ServiceItems.Select(i => new WorkOrderServiceItemDto(i.ServiceId, i.ServiceName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            wo.PartItems.Select(i => new WorkOrderPartItemDto(i.PartId, i.PartName, i.PartSku, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            wo.StatusHistory.Select(h => new WorkOrderStatusHistoryDto(h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedBy)).ToList(),
            wo.CreatedAt);
    }

    public static WorkOrderStatusDto ToStatusDto(this WorkOrder wo) =>
        new(wo.Id, wo.OrderNumber, wo.Status, wo.Status.ToPortuguese());

    public static TrackingWorkOrderDto ToTrackingDto(this WorkOrder wo) =>
        new(
            wo.OrderNumber,
            wo.Status,
            wo.Status.ToPortuguese(),
            wo.TotalAmount,
            wo.ServiceItems.Select(i => new WorkOrderServiceItemDto(i.ServiceId, i.ServiceName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            wo.PartItems.Select(i => new WorkOrderPartItemDto(i.PartId, i.PartName, i.PartSku, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            wo.StatusHistory.Select(h => new WorkOrderStatusHistoryDto(h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedBy)).ToList(),
            wo.CreatedAt);
}
