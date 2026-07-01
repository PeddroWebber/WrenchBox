using FluentAssertions;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Domain.Tests.ValueObjects;

public class DocumentTests
{
    [Theory]
    [InlineData("390.533.447-05")]
    [InlineData("39053344705")]
    public void Create_ValidCpf_Succeeds(string input)
    {
        var doc = Document.Create(input);
        doc.Type.Should().Be(DocumentType.Cpf);
        doc.Value.Should().Be("39053344705");
    }

    [Theory]
    [InlineData("04.252.011/0001-10")]
    [InlineData("04252011000110")]
    public void Create_ValidCnpj_Succeeds(string input)
    {
        var doc = Document.Create(input);
        doc.Type.Should().Be(DocumentType.Cnpj);
        doc.Value.Should().Be("04252011000110");
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("12345678901")]
    [InlineData("")]
  public void Create_InvalidCpf_Throws(string input)
    {
        var act = () => Document.Create(input);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_InvalidCnpj_Throws()
    {
        var act = () => Document.Create("11111111111111");
        act.Should().Throw<DomainException>();
    }
}
