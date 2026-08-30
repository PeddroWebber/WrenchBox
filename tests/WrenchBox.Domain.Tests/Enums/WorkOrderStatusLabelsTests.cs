using FluentAssertions;
using WrenchBox.Domain.Enums;

namespace WrenchBox.Domain.Tests.Enums;

public class WorkOrderStatusLabelsTests
{
    [Theory]
    [InlineData(WorkOrderStatus.Received, "Recebida")]
    [InlineData(WorkOrderStatus.InDiagnosis, "Diagnóstico")]
    [InlineData(WorkOrderStatus.AwaitingApproval, "Aguardando Aprovação")]
    [InlineData(WorkOrderStatus.InExecution, "Execução")]
    [InlineData(WorkOrderStatus.Completed, "Finalizada")]
    [InlineData(WorkOrderStatus.Delivered, "Entregue")]
    public void ToPortuguese_ReturnsRequiredLabel(WorkOrderStatus status, string expected)
    {
        status.ToPortuguese().Should().Be(expected);
    }

    [Fact]
    public void QueuePriority_OrdersActiveStatusesAsRequired()
    {
        WorkOrderStatus.InExecution.QueuePriority().Should().BeLessThan(WorkOrderStatus.AwaitingApproval.QueuePriority());
        WorkOrderStatus.AwaitingApproval.QueuePriority().Should().BeLessThan(WorkOrderStatus.InDiagnosis.QueuePriority());
        WorkOrderStatus.InDiagnosis.QueuePriority().Should().BeLessThan(WorkOrderStatus.Received.QueuePriority());
        WorkOrderStatus.Completed.QueuePriority().Should().BeGreaterThan(WorkOrderStatus.Received.QueuePriority());
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed, true)]
    [InlineData(WorkOrderStatus.Delivered, true)]
    [InlineData(WorkOrderStatus.Received, false)]
    [InlineData(WorkOrderStatus.InExecution, false)]
    public void IsClosed_MatchesLogicalExclusion(WorkOrderStatus status, bool expected)
    {
        status.IsClosed().Should().Be(expected);
    }
}
