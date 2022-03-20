using OpenStatusPage.Server.Domain.Entities;

namespace OpenStatusPage.Server.Application.Misc
{
    public static class EntityExtensions
    {
        public static string GetRealEntityTypeString(this EntityBase entity)
        {
            return entity.GetType().Namespace.StartsWith("Castle.Proxies") ? entity.GetType().BaseType.Name : entity.GetType().Name;
        }

        public static Type GetRealEntityType(this EntityBase entity)
        {
            return entity.GetType().Namespace.StartsWith("Castle.Proxies") ? entity.GetType().BaseType : entity.GetType();
        }
    }
}
