using FluentAssertions;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Domain.Tests.ValueObjects;

public class DocumentAdditionalTests
{
    [Fact]
    public void TryCreate_Invalid_ReturnsFalse()
    {
        var result = Document.TryCreate("invalid", out var doc);
        result.Should().BeFalse();
        doc.Should().BeNull();
    }

    [Fact]
    public void Formatted_Cpf_ReturnsMasked()
    {
        var doc = Document.Create("39053344705");
        doc.Formatted.Should().Be("390.533.447-05");
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        var a = Document.Create("39053344705");
        var b = Document.Create("390.533.447-05");
        a.Equals(b).Should().BeTrue();
    }
}

public class PlateAdditionalTests
{
    [Fact]
    public void Formatted_AddsHyphen()
    {
        var plate = Plate.Create("ABC1234");
        plate.Formatted.Should().Be("ABC-1234");
    }

    [Fact]
    public void TryCreate_Invalid_ReturnsFalse()
    {
        var result = Plate.TryCreate("INVALID", out var plate);
        result.Should().BeFalse();
        plate.Should().BeNull();
    }
}
