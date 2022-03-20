using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Shared.DataTransferObjects.Services;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests.Services;
using System.ComponentModel.DataAnnotations;
using static OpenStatusPage.Shared.DataTransferObjects.Services.ServiceStatusHistorySegmentDto;

namespace OpenStatusPage.Server.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ServiceStatusHistoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceStatusHistoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("public/bulk")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceStatusHistoryRequest.Response>> GetStatusHistoryForServicesAsync([FromBody, Required] ServiceStatusHistoryRequest request)
    {
        try
        {
            var response = new ServiceStatusHistoryRequest.Response()
            {
                ServiceStatusHistories = new()
            };

            var sericeIds = request.ServiceIds.Distinct().ToList();

            foreach (var monitorId in sericeIds)
            {
                //Get the first record that describes the status at the earliest point of the request
                //This record might preceed the request from date time, hence it must be fetched extra
                var firstRecord = await _mediator.Send(new GetStatusFromHistoryCmd()
                {
                    MonitorId = monitorId,
                    UtcAt = request.From.UtcDateTime
                });

                //If we have a history record just before the search window, start with that
                var queryFrom = firstRecord?.FromUtc ?? request.From.UtcDateTime;

                //Get additional records that come after the first until the max request time frame
                var records = (await _mediator.Send(new StatusHistoriesQuery()
                {
                    Query = new(query => query
                        .Where(x => x.MonitorId == monitorId && queryFrom <= x.FromUtc && x.FromUtc <= request.Until.UtcDateTime)
                        .OrderBy(x => x.FromUtc))
                }))?.HistoryRecords ?? new();

                ServiceStatusHistorySegmentDto currentSegment = null!;
                Outage currentOutage = null!;

                foreach (var record in records)
                {
                    //End current outage if the status changes
                    if (currentSegment != null && currentOutage != null && currentOutage.ServiceStatus != record.Status)
                    {
                        currentOutage.Until = record.FromUtc;
                        currentSegment.Outages.Add(currentOutage);
                        currentOutage = null!;
                    }

                    //End current segment if we enter unknown state
                    if (currentSegment != null && record.Status == ServiceStatus.Unknown)
                    {
                        currentSegment.Until = record.FromUtc;
                        response.ServiceStatusHistories.Add(currentSegment);
                        currentSegment = null!;
                    }

                    //Add a new segment if we have known data
                    if (currentSegment == null && record.Status != ServiceStatus.Unknown)
                    {
                        currentSegment = new()
                        {
                            ServiceId = monitorId,
                            From = record.FromUtc,
                            Outages = new()
                        };
                    }

                    //Add a new outage for non available status records
                    if (currentSegment != null && currentOutage == null && record.Status != ServiceStatus.Available)
                    {
                        currentOutage = new()
                        {
                            From = record.FromUtc,
                            ServiceStatus = record.Status
                        };
                    }
                }

                //Close and add last segment
                if (currentSegment != null)
                {
                    //Add ongoing outage at the end if there is one
                    if (currentOutage != null)
                    {
                        currentSegment.Outages.Add(currentOutage);
                    }

                    //Append ongoing history segment, so no until timestamp
                    response.ServiceStatusHistories.Add(currentSegment);
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem();
        }
    }
}
