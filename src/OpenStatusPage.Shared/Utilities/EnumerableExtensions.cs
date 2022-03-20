namespace OpenStatusPage.Shared.Utilities
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<(T, T)> PairwiseLeadingDefault<T>(this IEnumerable<T> source)
        {
            var prev = default(T);

            using var enumerator = source.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return (prev!, prev = enumerator.Current);
            }
        }
    }
}
