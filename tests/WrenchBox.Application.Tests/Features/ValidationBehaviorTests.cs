using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using WrenchBox.Application.Behaviors;
using WrenchBox.Application.Features.Auth;

namespace WrenchBox.Application.Tests.Features;

public record TestRequest(string Value) : IRequest<string>;

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator() => RuleFor(x => x.Value).NotEmpty();
}

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var result = await behavior.Handle(new TestRequest("x"), _ => Task.FromResult("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var validator = new TestRequestValidator();
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var result = await behavior.Handle(new TestRequest("valid"), _ => Task.FromResult("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        var validator = new TestRequestValidator();
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var act = async () => await behavior.Handle(
            new TestRequest(""),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}

public class ValidatorTests
{
    [Fact]
    public void LoginCommandValidator_InvalidEmail_Fails()
    {
        var validator = new LoginCommandValidator();
        var result = validator.Validate(new LoginCommand("", "pass"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginCommandValidator_ValidInput_Passes()
    {
        var validator = new LoginCommandValidator();
        var result = validator.Validate(new LoginCommand("admin@test.com", "pass"));
        result.IsValid.Should().BeTrue();
    }
}
