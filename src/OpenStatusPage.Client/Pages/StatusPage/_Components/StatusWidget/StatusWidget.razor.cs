using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using OpenStatusPage.Shared.DataTransferObjects.Services;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Globalization;
using static OpenStatusPage.Shared.DataTransferObjects.Services.ServiceStatusHistorySegmentDto;

namespace OpenStatusPage.Client.Pages.StatusPage._Components.StatusWidget;

public partial class StatusWidget
{
    /// <summary>
    /// All the data to display
    /// </summary>
    [Parameter]
    public List<ServiceStatusHistorySegmentDto> Segments { get; set; }

    /// <summary>
    /// How many days to display
    /// </summary>
    [Parameter]
    public int Days { get; set; } = 90;

    /// <summary>
    /// Show details on the outage that affected the day being inspected
    /// </summary>
    [Parameter]
    public bool ShowOutageDetails { get; set; }

    [Inject]
    protected IBreakpointService BreakpointListener { get; set; }

    protected Dictionary<int, string> ClassPerDay { get; set; }

    protected Dictionary<int, Dictionary<string, TimeSpan>> UnavailablePerDayPerService { get; set; }

    protected Dictionary<int, Dictionary<string, TimeSpan>> DegradedPerDayPerService { get; set; }

    protected DateTimeOffset LastNowProcessed { get; set; }

    protected ServiceStatus LastServiceStatus { get; set; }

    protected Guid _subscriptionId;

    protected int _daysVisible;

    protected string _uptimePercentage;

    protected override async Task OnInitializedAsync()
    {
        _daysVisible = GetVisibleDays(await BreakpointListener.GetBreakpoint());

        //Resize events
        _subscriptionId = (await BreakpointListener.Subscribe(async (breakpoint) =>
        {
            var newDaysVisible = GetVisibleDays(breakpoint);

            if (newDaysVisible != _daysVisible)
            {
                _daysVisible = newDaysVisible;

                await RefreshDataAsync();

                await InvokeAsync(StateHasChanged);
            }

        }, new ResizeOptions { ReportRate = 50, NotifyOnBreakpointOnly = false, SuppressInitEvent = false })).SubscriptionId;

        await RefreshDataAsync();

        await base.OnInitializedAsync();
    }

    protected int GetVisibleDays(Breakpoint breakpoint)
    {
        var maxDays = breakpoint switch
        {
            Breakpoint.Xs => 14,
            Breakpoint.Sm => 30,
            _ => 90,
        };

        return Math.Min(Days, maxDays);
    }

    protected async Task RefreshDataAsync()
    {
        UnavailablePerDayPerService = new();
        DegradedPerDayPerService = new();
        ClassPerDay = new();

        if (Segments == null || Segments.Count == 0) return;

        var serviceIds = Segments
            .Select(x => x.ServiceId)
            .Distinct()
            .ToList();

        var from = DateTimeOffset.Now.Date.ToUniversalTime();

        for (int nDay = 0; nDay < _daysVisible; nDay++)
        {
            DateTimeOffset day = from.AddDays(-_daysVisible + nDay + 1);
            var dayEnd = day.AddDays(1);

            var segements = Segments.Where(x => day.IsInRangeInclusiveNullable(x.From.Date, x.Until?.Date)).ToList();

            if (segements.Count == 0 || !serviceIds.All(serviceId => segements.Any(x => x.ServiceId == serviceId)))
            {
                continue; //No data
            }
            else
            {
                var outages = segements
                    .SelectMany(x => x.Outages)
                    .Where(x => day.IsInRangeInclusiveNullable(x.From.Date, x.Until.HasValue ? x.Until.Value.Date : null))
                    .OrderBy(x => x.From)
                    .ToList();

                var worstOutage = outages.Count > 0 ? outages
                    .Max(x => x.ServiceStatus) : ServiceStatus.Available;

                if (worstOutage == ServiceStatus.Unknown) continue; //No valid data for the day

                ClassPerDay[nDay] = worstOutage switch
                {
                    ServiceStatus.Available => "status-color-no-outage",
                    ServiceStatus.Degraded => "status-color-partial-outage",
                    ServiceStatus.Unavailable => "status-color-full-outage",
                    _ => throw new InvalidDataException()
                };

                foreach (var serviceId in serviceIds)
                {
                    var serviceOutages = outages.Where(x => Segments.Any(y => y.ServiceId == serviceId && y.Outages.Contains(x)));
                    var now = DateTimeOffset.UtcNow;

                    //Add up all the ms of unavailable status of the day
                    double sumUnavialable = 0;

                    foreach (var outageUnavailable in MergeOverlappingOutages(serviceOutages.Where(x => x.ServiceStatus == ServiceStatus.Unavailable)))
                    {
                        var outageFrom = new[] { outageUnavailable.From, day }.Max();
                        var outageUntil = new[] { outageUnavailable.Until ?? now, now, dayEnd }.Min();

                        sumUnavialable += (outageUntil - outageFrom).TotalMilliseconds;

                        //Stop when the history is caught up with
                        if (outageUntil >= now) break;
                    }

                    if (sumUnavialable > 0)
                    {
                        if (!UnavailablePerDayPerService.ContainsKey(nDay)) UnavailablePerDayPerService[nDay] = new();

                        UnavailablePerDayPerService[nDay][serviceId] = TimeSpan.FromMilliseconds(sumUnavialable);
                    }

                    //Add up all the ms of degraded status of the day
                    double sumDegraded = 0;

                    foreach (var outageDegraded in MergeOverlappingOutages(serviceOutages.Where(x => x.ServiceStatus == ServiceStatus.Degraded)))
                    {
                        var outageFrom = new[] { outageDegraded.From, day }.Max();

                        var outageUntil = new[] { outageDegraded.Until ?? now, now, dayEnd }.Min();

                        sumDegraded += (outageUntil - outageFrom).TotalMilliseconds;

                        //Stop when the history is caught up with
                        if (outageUntil >= now) break;
                    }

                    if (sumDegraded > 0)
                    {
                        if (!DegradedPerDayPerService.ContainsKey(nDay)) DegradedPerDayPerService[nDay] = new();

                        DegradedPerDayPerService[nDay][serviceId] = TimeSpan.FromMilliseconds(sumDegraded);
                    }
                }
            }
        }

        RefreshLastStatus();
        RecalculacteUptimePercentage();
    }

    protected void RefreshLastStatus()
    {
        LastNowProcessed = DateTimeOffset.UtcNow;

        LastServiceStatus = ServiceStatus.Unknown;

        if (Segments != null && Segments.Count > 0)
        {
            var outages = Segments
                .SelectMany(x => x.Outages)
                .Where(x => LastNowProcessed.IsInRangeInclusiveNullable(x.From, x.Until))
                .ToList();

            if (outages.Count > 0)
            {
                LastServiceStatus = outages.Max(x => x.ServiceStatus);
            }
            else
            {
                //No outages that are happening right now but we had data
                LastServiceStatus = ServiceStatus.Available;
            }
        }
    }

    protected void RecalculacteUptimePercentage()
    {
        _uptimePercentage = null!;

        if (Segments == null || Segments.Count == 0 || UnavailablePerDayPerService == null || DegradedPerDayPerService == null) return;

        double msData = 0;
        double msOutage = 0;

        for (int nDay = 0; nDay < _daysVisible; nDay++)
        {
            if (HasData(nDay))
            {
                if (nDay + 1 < _daysVisible)
                {
                    //Add full days for for all but the last day
                    msData += TimeSpan.FromDays(1).TotalMilliseconds;
                }
                else
                {
                    //For the last day add the ms passed so far on that day
                    msData += (DateTimeOffset.UtcNow - DateTimeOffset.UtcNow.UtcDateTime.Date).TotalMilliseconds;
                }
            }

            //Added unavailable and degraded ms or 0 if no known data for the day. Use max ms distinct by service for aggregated widgets

            var unavailable = UnavailablePerDayPerService.GetValueOrDefault(nDay)?.MaxBy(x => x.Value).Value.TotalMilliseconds ?? 0;
            var degraded = DegradedPerDayPerService.GetValueOrDefault(nDay)?.MaxBy(x => x.Value).Value.TotalMilliseconds ?? 0;

            msOutage += Math.Min(unavailable + degraded, TimeSpan.FromDays(1).TotalMilliseconds);
        }

        var uptimePercentage = Math.Clamp((1.0 - msOutage / msData) * 100, 0, 100);

        _uptimePercentage = uptimePercentage.ToString("0.00");
    }

    protected string GetUptimePercentageString()
    {
        return _uptimePercentage ?? "0.00";
    }

    protected bool HasData(int day)
    {
        return ClassPerDay != null && ClassPerDay.ContainsKey(day);
    }

    protected string GetCssClass(int day)
    {
        var css = "mud-height-full mud-paper-square " + ClassPerDay.GetValueOrDefault(day, "status-color-nodata");

        if (HasData(day)) css += " status-bar-segment-zoom status-bar-tooltip";

        return css;
    }

    protected string GetDayString(int day)
    {
        return DateTimeOffset.Now.Date.AddDays(-_daysVisible + day + 1).ToString("d", CultureInfo.CurrentUICulture);
    }

    protected static bool OutageOverlaps(Outage a, Outage b)
    {
        return a.From <= b.From ? (!a.Until.HasValue || a.Until.Value >= b.From) : (!b.Until.HasValue || b.Until >= a.From);
    }

    protected static Outage MergeOutages(Outage a, Outage b)
    {
        return new Outage
        {
            From = new[] { a.From, b.From }.Min(),
            Until = (!a.Until.HasValue || b.Until.HasValue) ? null : new[] { a.Until, b.Until }.Min(),
            ServiceStatus = new[] { a.ServiceStatus, b.ServiceStatus }.Min()
        };
    }

    public static IEnumerable<Outage> MergeOverlappingOutages(IEnumerable<Outage> source)
    {
        var enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            yield break;
        }

        var current = enumerator.Current;

        while (enumerator.MoveNext())
        {
            var next = enumerator.Current;

            if (!OutageOverlaps(current, next))
            {
                yield return current;

                current = next;
            }
            else
            {
                current = MergeOutages(current, next);
            }
        }

        yield return current;
    }
}
