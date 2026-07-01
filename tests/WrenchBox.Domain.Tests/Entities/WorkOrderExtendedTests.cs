using FluentAssertions;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Domain.Tests.Entities;

public class WorkOrderExtendedTests
{
    private static Service CreateService(decimal price = 100m, bool active = true)
    {
        var s = Service.Create("Svc", "D", price, 30);
        if (!active) s.Update("Svc", "D", price, 30, false);
        return s;
    }

    private static Part CreatePart(int stock = 10, bool active = true)
    {
        var p = Part.Create("P", "SKU1", 10m, stock, 1);
        if (!active) p.Update("P", 10m, 1, false);
        return p;
    }

    [Fact]
    public void Create_InactiveService_Throws()
    {
        var act = () => WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(),
            [(CreateService(active: false), 1)], []);
        act.Should().Throw<DomainException>().WithMessage("*not active*");
    }

    [Fact]
    public void Create_InactivePart_Throws()
    {
        var act = () => WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(),
            [(CreateService(), 1)], [(CreatePart(active: false), 1)]);
        act.Should().Throw<DomainException>().WithMessage("*not active*");
    }

    [Fact]
    public void Complete_FromWrongStatus_Throws()
    {
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(), [(CreateService(), 1)], []);
        var act = () => order.Complete();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deliver_FromWrongStatus_Throws()
    {
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(), [(CreateService(), 1)], []);
        var act = () => order.Deliver();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ApproveBudget_FromWrongStatus_Throws()
    {
        var order = WorkOrder.Create("WO-1", Guid.NewGuid(), Guid.NewGuid(), [(CreateService(), 1)], []);
        var act = () => order.ApproveBudget(new Dictionary<Guid, Part>());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdjustStock_IncreasesQuantity()
    {
        var part = Part.Create("P", "SKU", 5m, 5, 2);
        part.AdjustStock(3, "Restock");
        part.StockQuantity.Should().Be(8);
    }
}

public class DocumentCnpjTests
{
    [Fact]
    public void Create_ValidCnpj_Succeeds()
    {
        var doc = Document.Create("04252011000110");
        doc.Type.Should().Be(DocumentType.Cnpj);
        doc.Formatted.Should().Contain("/");
    }
}

public class CustomerValidationTests
{
    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => Customer.Create("39053344705", "", "a@b.com", "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_EmptyEmail_Throws()
    {
        var c = Customer.Create("39053344705", "Name", "a@b.com", "");
        var act = () => c.Update("Name", "", "");
        act.Should().Throw<DomainException>();
    }
}
