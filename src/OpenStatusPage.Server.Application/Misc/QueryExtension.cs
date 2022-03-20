namespace OpenStatusPage.Server.Application.Misc
{
    public class QueryExtension<T> where T : class
    {
        public QueryExtension(Func<IQueryable<T>, IQueryable<T>> extension)
        {
            Extension = extension;
        }

        public Func<IQueryable<T>, IQueryable<T>> Extension { get; }

        public IQueryable<T> ApplyTo(IQueryable<T> query)
        {
            if (Extension != null)
            {
                return Extension.Invoke(query);
            }
            else
            {
                return query;
            }
        }

        public static implicit operator QueryExtension<T>(Func<IQueryable<T>, IQueryable<T>> include)
        {
            return new QueryExtension<T>(include);
        }
    }
}
