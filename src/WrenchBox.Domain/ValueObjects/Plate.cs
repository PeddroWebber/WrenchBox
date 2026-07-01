using System.Text.RegularExpressions;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.ValueObjects;

public sealed class Plate : IEquatable<Plate>
{
  private static readonly Regex LegacyPattern = new(@"^[A-Z]{3}[0-9]{4}$", RegexOptions.Compiled);
  private static readonly Regex MercosulPattern = new(@"^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.Compiled);

  public string Value { get; }

  private Plate(string value) => Value = value;

  public static Plate Create(string rawValue)
  {
    if (string.IsNullOrWhiteSpace(rawValue))
      throw new DomainException("Vehicle plate is required.");

    var normalized = Regex.Replace(rawValue.ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);

    if (!LegacyPattern.IsMatch(normalized) && !MercosulPattern.IsMatch(normalized))
      throw new DomainException("Invalid vehicle plate. Use legacy (ABC1234) or Mercosul (ABC1D23) format.");

    return new Plate(normalized);
  }

  public static bool TryCreate(string rawValue, out Plate? plate)
  {
    try
    {
      plate = Create(rawValue);
      return true;
    }
    catch (DomainException)
    {
      plate = null;
      return false;
    }
  }

  public string Formatted => Value.Length == 7
    ? $"{Value[..3]}-{Value[3..]}"
    : Value;

  public override string ToString() => Value;

  public bool Equals(Plate? other) => other is not null && Value == other.Value;

  public override bool Equals(object? obj) => obj is Plate other && Equals(other);

  public override int GetHashCode() => Value.GetHashCode();
}
