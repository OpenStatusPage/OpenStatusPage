namespace OpenStatusPage.Server.Application.Misc.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RequiresDbTransactionAttribute : Attribute
    {
    }
}
