using Microsoft.AspNetCore.Components;

namespace OpenStatusPage.Client.Pages._Components
{
    public partial class DataLoader
    {
        [Parameter]
        public object WaitFor { get; set; }

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}