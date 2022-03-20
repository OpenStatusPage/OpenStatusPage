namespace OpenStatusPage.Server.Application.Misc
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, QueryExtension<T> queryExtension) where T : class
        {
            if (queryExtension != null)
            {
                return queryExtension.ApplyTo(queryable);
            }

            return queryable;
        }
    }
}
