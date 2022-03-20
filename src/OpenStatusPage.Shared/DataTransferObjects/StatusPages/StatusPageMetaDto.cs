namespace OpenStatusPage.Shared.DataTransferObjects.StatusPages
{
    /// <summary>
    /// Minimal meta data to show status pages in list views
    /// </summary>
    public class StatusPageMetaDto : EntityBaseDto
    {
        public string Name { get; set; }

        public bool IsPublic { get; set; }
    }
}
