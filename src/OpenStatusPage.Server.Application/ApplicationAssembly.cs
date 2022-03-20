namespace OpenStatusPage.Server.Application
{
    /// <summary>
    /// Compile time assembly reference for the application project, so that it can be used as ApplicationAssembly.Reference
    /// </summary>
    public static class ApplicationAssembly
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211", Justification = "Can not make this const")]
        public static System.Reflection.Assembly Reference = typeof(ApplicationAssembly).Assembly;
    }
}
