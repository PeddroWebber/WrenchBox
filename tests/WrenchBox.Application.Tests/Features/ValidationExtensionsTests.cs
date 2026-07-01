using FluentAssertions;
using FluentValidation;
using WrenchBox.Application.Features.Customers;
using WrenchBox.Application.Validators;

namespace WrenchBox.Application.Tests.Features;

public class ValidationExtensionsTests
{
    private class TestModel
    {
        public string Document { get; set; } = string.Empty;
        public string Plate { get; set; } = string.Empty;
    }

    private class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator()
        {
            RuleFor(x => x.Document).ValidDocument();
            RuleFor(x => x.Plate).ValidPlate();
        }
    }

    [Fact]
    public void ValidDocument_AcceptsValidCpf()
    {
        var validator = new TestModelValidator();
        var result = validator.Validate(new TestModel { Document = "39053344705", Plate = "ABC1D23" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidDocument_RejectsInvalidCpf()
    {
        var validator = new TestModelValidator();
        var result = validator.Validate(new TestModel { Document = "11111111111", Plate = "ABC1D23" });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidPlate_RejectsInvalidFormat()
    {
        var validator = new TestModelValidator();
        var result = validator.Validate(new TestModel { Document = "39053344705", Plate = "INVALID" });
        result.IsValid.Should().BeFalse();
    }
}
