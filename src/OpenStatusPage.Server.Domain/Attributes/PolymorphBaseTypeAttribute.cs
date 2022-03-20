using System;

namespace OpenStatusPage.Server.Domain.Attributes
{
    /// <summary>
    /// Declares a base type for polymorph entites to assist the persistence handling in decision making about table creations and relations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class PolymorphBaseTypeAttribute : Attribute
    {
    }
}
