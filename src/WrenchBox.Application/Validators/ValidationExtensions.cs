using FluentValidation;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Application.Validators;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ValidDocument<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.Must(d => Document.TryCreate(d, out _))
            .WithMessage("Document must be a valid CPF or CNPJ.");

    public static IRuleBuilderOptions<T, string> ValidPlate<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.Must(p => Plate.TryCreate(p, out _))
            .WithMessage("Plate must be a valid legacy (ABC1234) or Mercosul (ABC1D23) format.");
}
