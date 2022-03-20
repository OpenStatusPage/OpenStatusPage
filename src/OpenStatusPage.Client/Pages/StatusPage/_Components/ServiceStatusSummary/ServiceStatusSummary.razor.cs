using Microsoft.AspNetCore.Components;
using OpenStatusPage.Shared.DataTransferObjects.Services;
using static OpenStatusPage.Shared.DataTransferObjects.StatusPages.StatusPageDto;

namespace OpenStatusPage.Client.Pages.StatusPage._Components.ServiceStatusSummary
{
    public partial class ServiceStatusSummary
    {
        /// <summary>
        /// How many days the history should show.
        /// </summary>
        [Parameter]
        public int Days { get; set; } = 90;

        /// <summary>
        /// Title of the summary
        /// </summary>
        [Parameter]
        public MonitorSummary Summary { get; set; }

        /// <summary>
        /// History data for the services represented in this summary
        /// </summary>
        [Parameter]
        public List<ServiceStatusHistorySegmentDto> ServiceHistories { get; set; }

        protected bool IsExpanded { get; set; }

        protected static string ComputeHistoryKey(List<ServiceStatusHistorySegmentDto> histories)
        {
            var serviceIds = histories?.Select(x => x.ServiceId).Distinct().ToList() ?? new();

            var latestData = histories?.MaxBy(x => x.From)?.From.ToString() ?? "NODATETIME";

            return string.Join('_', serviceIds) + latestData;
        }
    }
}
