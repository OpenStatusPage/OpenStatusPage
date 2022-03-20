namespace OpenStatusPage.Server.Domain.Interfaces
{
    public interface IPolymorph
    {
        public static string GetTypeAsMulitpleString<T>() where T : class
        {
            return GetTypeAsMulitpleString(typeof(T));
        }

        public static string GetTypeAsMulitpleString(System.Type type)
        {
            var typeString = type.Name.ToString();

            if (typeString.EndsWith("y")) return $"{typeString[0..^1]}ies";

            return $"{typeString}s";
        }
    }
}
