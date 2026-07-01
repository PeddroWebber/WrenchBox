using FluentAssertions;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Tests.Entities;

public class CustomerTests
{
    [Fact]
    public void Create_ValidData_Succeeds()
    {
        var customer = Customer.Create("39053344705", "Jane", "jane@test.com", "11999999999");
        customer.Document.Should().Be("39053344705");
        customer.Name.Should().Be("Jane");
    }

    [Fact]
    public void Create_InvalidDocument_Throws()
    {
        var act = () => Customer.Create("invalid", "Jane", "jane@test.com", "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_ChangesFields()
    {
        var customer = Customer.Create("39053344705", "Jane", "jane@test.com", "");
        customer.Update("Jane Updated", "new@test.com", "11888888888");
        customer.Name.Should().Be("Jane Updated");
        customer.Email.Should().Be("new@test.com");
    }

    [Fact]
    public void AddVehicle_AddsToCollection()
    {
        var customer = Customer.Create("39053344705", "Jane", "jane@test.com", "");
        var vehicle = customer.AddVehicle("ABC1D23", "Ford", "Focus", 2019);
        customer.Vehicles.Should().Contain(vehicle);
    }
}

public class VehicleTests
{
    [Fact]
    public void Create_ValidData_Succeeds()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "ABC1234", "VW", "Golf", 2018);
        vehicle.Plate.Should().Be("ABC1234");
    }

    [Fact]
    public void Create_InvalidYear_Throws()
    {
        var act = () => Vehicle.Create(Guid.NewGuid(), "ABC1234", "VW", "Golf", 1800);
        act.Should().Throw<DomainException>();
    }
}

public class ServiceTests
{
    [Fact]
    public void Create_AndUpdate_Work()
    {
        var service = Service.Create("Test", "Desc", 99m, 30);
        service.Update("Updated", "New", 120m, 45, false);
        service.Name.Should().Be("Updated");
        service.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_NegativePrice_Throws()
    {
        var act = () => Service.Create("Test", "", -1m, 30);
        act.Should().Throw<DomainException>();
    }
}
