using Microsoft.AspNetCore.Components;
using OpenStatusPage.Shared.DataTransferObjects.Services;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;

namespace OpenStatusPage.Client.Pages.StatusPage._Components.ServiceStatusSummary
{
    public partial class CurrentStatusWidget
    {
        /// <summary>
        /// History data for this indivudual service
        /// </summary>
        [Parameter]
        public List<ServiceStatusHistorySegmentDto>? ServiceHistory { get; set; }

        public ServiceStatus CurrentOutageStatus { get; set; }

        protected override async Task OnInitializedAsync()
        {
            RefreshData();

            await base.OnInitializedAsync();
        }

        protected void RefreshData()
        {
            CurrentOutageStatus = ServiceStatus.Unknown;

            if (ServiceHistory != null && ServiceHistory.Count > 0)
            {
                var outages = ServiceHistory
                    .SelectMany(x => x.Outages)
                    .Where(x => DateTimeOffset.UtcNow.IsInRangeInclusiveNullable(x.From, x.Until))
                    .ToList();

                if (outages.Count > 0)
                {
                    CurrentOutageStatus = outages.Max(x => x.ServiceStatus);
                }
                else
                {
                    //No outages that are happening right now but we had data
                    CurrentOutageStatus = ServiceStatus.Available;
                }
            }
        }
    }
}
