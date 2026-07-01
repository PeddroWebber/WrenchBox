using FluentAssertions;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Tests.Entities;

public class PartTests
{
    [Fact]
    public void Deduct_ReducesStock()
    {
        var part = Part.Create("Oil Filter", "OF-001", 25m, 10, 2);
        part.Deduct(3, null, "Test");
        part.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void Deduct_InsufficientStock_Throws()
    {
        var part = Part.Create("Oil Filter", "OF-001", 25m, 2, 1);
        var act = () => part.Deduct(5, null, "Test");
        act.Should().Throw<DomainException>().WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void AdjustStock_NegativeBelowZero_Throws()
    {
        var part = Part.Create("Oil Filter", "OF-001", 25m, 5, 1);
        var act = () => part.AdjustStock(-10, "Correction");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsBelowMinimumStock_ReturnsTrue_WhenLow()
    {
        var part = Part.Create("Oil Filter", "OF-001", 25m, 1, 5);
        part.IsBelowMinimumStock().Should().BeTrue();
    }
}
