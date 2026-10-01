namespace HotelReservations.Queries;

/// <summary>
/// Levenshtein distance, so a clerk who mistypes a character still finds the reservation.
/// </summary>
public static class FuzzyMatcher
{
    public const int MaxDistance = 2;

    public static int Distance(string a, string b)
    {
        a = a.ToUpperInvariant();
        b = b.ToUpperInvariant();
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
