using FluentAssertions;
using FluentValidation;
using WrenchBox.Application.Features.Customers;
using WrenchBox.Application.Features.Parts;
using WrenchBox.Application.Features.Services;
using WrenchBox.Application.Features.Vehicles;
using WrenchBox.Application.Features.WorkOrders;

namespace WrenchBox.Application.Tests.Features;

public class CommandValidatorTests
{
    [Fact]
    public void CreateCustomerValidator_InvalidDocument_Fails()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.Validate(new CreateCustomerCommand("invalid", "Name", "a@b.com", "119"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCustomerValidator_ValidInput_Passes()
    {
        var validator = new CreateCustomerCommandValidator();
        var result = validator.Validate(new CreateCustomerCommand("39053344705", "João", "a@b.com", "119"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateCustomerValidator_EmptyName_Fails()
    {
        var validator = new UpdateCustomerCommandValidator();
        var result = validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), "", "a@b.com", "119"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateVehicleValidator_InvalidPlate_Fails()
    {
        var validator = new CreateVehicleCommandValidator();
        var result = validator.Validate(new CreateVehicleCommand(Guid.NewGuid(), "INVALID", "Toyota", "Corolla", 2020));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateVehicleValidator_ValidInput_Passes()
    {
        var validator = new CreateVehicleCommandValidator();
        var result = validator.Validate(new CreateVehicleCommand(Guid.NewGuid(), "ABC1D23", "Toyota", "Corolla", 2020));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePartValidator_NegativePrice_Fails()
    {
        var validator = new CreatePartCommandValidator();
        var result = validator.Validate(new CreatePartCommand("Part", "SKU", -1m, 10, 5));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateServiceValidator_ZeroDuration_Fails()
    {
        var validator = new CreateServiceCommandValidator();
        var result = validator.Validate(new CreateServiceCommand("Service", "Desc", 10m, 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateWorkOrderValidator_EmptyServices_Fails()
    {
        var validator = new CreateWorkOrderCommandValidator();
        var result = validator.Validate(new CreateWorkOrderCommand(
            "39053344705", "João", "a@b.com", "119", "ABC1D23", "Toyota", "Corolla", 2020, [], [], null));
        result.IsValid.Should().BeFalse();
    }
}
