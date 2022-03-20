using Microsoft.AspNetCore.Components;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Globalization;

namespace OpenStatusPage.Client.Pages._Components.Incidents
{
    public partial class IncidentSummary
    {
        /// <summary>
        /// Parent status page data
        /// </summary>
        [Parameter]
        public StatusPageDto StatusPageConfiguration { get; set; }

        /// <summary>
        /// Incident data for the summary
        /// </summary>
        [Parameter]
        public IncidentDto Incident { get; set; }

        /// <summary>
        /// enable the preview for the latest status update to be visible in the header if collapsed
        /// </summary>
        [Parameter]
        public bool Preview { get; set; }

        protected bool IsExpanded { get; set; }

        protected string GetDateStartedString()
        {
            if (Incident.From > DateTimeOffset.UtcNow)
            {
                return $"Scheduled for {Incident.From.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture)}";
            }

            return $"Started {Incident.From.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture)}";
        }

        protected string GetDurationString()
        {
            var durationString = ((Incident.Until ?? DateTimeOffset.UtcNow) - Incident.From).DurationString();

            //Concluded incident in the past
            if (Incident.Until.HasValue && Incident.Until.Value <= DateTimeOffset.UtcNow)
            {
                return $"Lasted{durationString}";
            }

            //Future or current maintence handling
            if (Incident.Timeline.First().Severity == IncidentSeverity.Maintenance)
            {
                if (DateTimeOffset.UtcNow < Incident.From) //Future 
                {
                    if (Incident.Until.HasValue)
                    {
                        return $"Estimated to last{durationString}";
                    }
                    else
                    {
                        //No duration info yet
                        return "";
                    }
                }
                else if (Incident.Until.HasValue && DateTimeOffset.UtcNow < Incident.Until.Value) //Ongoing but end is set
                {
                    return $"Estimated to end in{(Incident.Until.Value - DateTimeOffset.UtcNow).DurationString()}";
                }
            }

            //Open incident with no known end date
            return $"Ongoing for{durationString}";
        }

        protected List<string> GetAffectedServicesStrings()
        {
            var result = new List<string>();

            foreach (var affectedService in Incident.AffectedServices)
            {
                var label = StatusPageConfiguration.MonitorSummaries
                    .SelectMany(x => x.LabeledMonitors)
                    .Where(x => x.MonitorId == affectedService)
                    .Select(x => x.Label)
                    .FirstOrDefault();

                result.Add(label ?? "Unknown service");
            }

            return result.OrderBy(x => x).ToList();
        }
    }
}
