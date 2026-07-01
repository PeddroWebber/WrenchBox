namespace WrenchBox.Infrastructure.Persistence;

internal static class SeedDataGenerator
{
    private const string PlateLetters = "ABCDEFGHJKLMNPRSTUVWXYZ";

    internal static string GenerateCpf(int seed)
    {
        if (seed is < 0 or >= 1_000_000_000)
            throw new ArgumentOutOfRangeException(nameof(seed), "Seed must fit in 9 CPF digits.");

        var digits = new int[9];
        var value = seed;
        for (var i = 8; i >= 0; i--)
        {
            digits[i] = value % 10;
            value /= 10;
        }

        if (digits.Distinct().Count() == 1)
            digits[8] = (digits[8] + 1) % 10;

        var d1 = CalculateCpfDigit(digits, 9);
        var d2 = CalculateCpfDigit([.. digits, d1], 10);
        return string.Concat(digits) + d1 + d2;
    }
    internal static string GenerateMercosulPlate(int index)
    {
        var l1 = PlateLetters[index % PlateLetters.Length];
        var l2 = PlateLetters[(index / 24) % PlateLetters.Length];
        var l3 = PlateLetters[(index / 576) % PlateLetters.Length];
        var digit = index % 10;
        var l4 = PlateLetters[(index / 10) % PlateLetters.Length];
        var d2 = (index / 100) % 10;
        var d3 = (index / 1000) % 10;
        return $"{l1}{l2}{l3}{digit}{l4}{d2}{d3}";
    }

    internal static T Pick<T>(this IReadOnlyCollection<T> items, int index) =>
        items is IReadOnlyList<T> list
            ? list[index % items.Count]
            : items.ElementAt(index % items.Count);
    private static int CalculateCpfDigit(int[] digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += digits[i] * (length + 1 - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
