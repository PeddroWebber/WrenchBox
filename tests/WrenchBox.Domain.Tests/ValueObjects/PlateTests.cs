using FluentAssertions;
using WrenchBox.Domain.Exceptions;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Domain.Tests.ValueObjects;

public class PlateTests
{
    [Theory]
    [InlineData("ABC1234")]
    [InlineData("abc-1234")]
    [InlineData("ABC1D23")]
    public void Create_ValidPlate_Succeeds(string input)
    {
        var plate = Plate.Create(input);
        plate.Value.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("AB1234")]
    [InlineData("ABCD1234")]
    [InlineData("")]
    public void Create_InvalidPlate_Throws(string input)
    {
        var act = () => Plate.Create(input);
        act.Should().Throw<DomainException>();
    }
}
