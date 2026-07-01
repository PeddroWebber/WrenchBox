using System.Text.RegularExpressions;
using WrenchBox.Domain.Enums;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.ValueObjects;

public sealed class Document : IEquatable<Document>
{
    public string Value { get; }
    public DocumentType Type { get; }

    private Document(string value, DocumentType type)
    {
        Value = value;
        Type = type;
    }

    public static Document Create(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            throw new DomainException("Document is required.");

        var digits = Regex.Replace(rawValue, @"\D", string.Empty);

        if (digits.Length == 11)
        {
            if (!IsValidCpf(digits))
                throw new DomainException("Invalid CPF.");

            return new Document(digits, DocumentType.Cpf);
        }

        if (digits.Length == 14)
        {
            if (!IsValidCnpj(digits))
                throw new DomainException("Invalid CNPJ.");

            return new Document(digits, DocumentType.Cnpj);
        }

        throw new DomainException("Document must be a valid CPF (11 digits) or CNPJ (14 digits).");
    }

    public static bool TryCreate(string rawValue, out Document? document)
    {
        try
        {
            document = Create(rawValue);
            return true;
        }
        catch (DomainException)
        {
            document = null;
            return false;
        }
    }

    public string Formatted => Type switch
    {
        DocumentType.Cpf => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}",
        DocumentType.Cnpj => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}",
        _ => Value
    };

    public override string ToString() => Value;

    public bool Equals(Document? other) =>
        other is not null && Value == other.Value && Type == other.Type;

    public override bool Equals(object? obj) => obj is Document other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Value, Type);

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (cpf[i] - '0') * (10 - i);

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if (cpf[9] - '0' != digit1)
            return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (cpf[i] - '0') * (11 - i);

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return cpf[10] - '0' == digit2;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
            return false;

        int[] multiplier1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] multiplier2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (cnpj[i] - '0') * multiplier1[i];

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if (cnpj[12] - '0' != digit1)
            return false;

        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += (cnpj[i] - '0') * multiplier2[i];

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return cnpj[13] - '0' == digit2;
    }
}
