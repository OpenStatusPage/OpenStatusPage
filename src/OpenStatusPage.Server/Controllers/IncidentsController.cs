using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Incidents;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Requests;
using OpenStatusPage.Shared.Requests.Incidents;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class IncidentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ClusterService _clusterService;

    public IncidentsController(IMediator mediator, IMapper mapper, ClusterService clusterService)
    {
        _mediator = mediator;
        _mapper = mapper;
        _clusterService = clusterService;
    }

    [HttpGet()]
    public async Task<ActionResult<List<IncidentMetaDto>>> GetAllAsync()
    {
        try
        {
            var searchResult = await _mediator.Send(new IncidentsQuery
            {
                Query = new(query => query.Include(x => x.Timeline))
            });

            var incidents = searchResult?.Incidents;

            if (incidents == null) Problem();

            var metas = new List<IncidentMetaDto>();

            foreach (var incident in incidents)
            {
                var latestTimelineItem = incident.Timeline.OrderBy(x => x.DateTime).LastOrDefault();

                metas.Add(new()
                {
                    Id = incident.Id,
                    Name = incident.Name,
                    Version = incident.Version,
                    LatestStatus = latestTimelineItem?.Status ?? IncidentStatus.Created,
                    LatestSeverity = latestTimelineItem?.Severity ?? IncidentSeverity.Information,
                    LastestTimelineItemId = latestTimelineItem?.Id!
                });
            }

            return Ok(metas);
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IncidentDto>> GetDetailsAsync(string id)
    {
        try
        {
            var searchResult = await _mediator.Send(new IncidentsQuery
            {
                Query = new(query => query
                    .Where(x => x.Id == id)
                    .Include(x => x.Timeline)
                    .Include(x => x.AffectedServices))
            });

            if (searchResult == null || searchResult.Incidents.Count > 1) return BadRequest();

            if (searchResult.Incidents.Count == 0) return NotFound();

            var incident = searchResult.Incidents[0];

            //Return ordered timeline
            incident.Timeline = incident.Timeline.OrderBy(x => x.DateTime).ToList();

            return Ok(_mapper.Map<IncidentDto>(incident));
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpPost()]
    public async Task<ActionResult<SuccessResponse>> CreateOrUpdateAsync()
    {
        try
        {
            var data = await Request.ReadFromJsonAsync<IncidentDto>();

            var incidentData = _mapper.Map<Incident>(data);

            var response = await _clusterService.ReplicateAsync(new CreateOrUpdateIncidentCmd()
            {
                Data = incidentData
            });

            if (!response) return Problem();

            return Ok(SuccessResponse.FromSuccess);
        }
        catch (Exception ex)
        {
            return Problem();
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<SuccessResponse>> DeleteAsync(string id)
    {
        try
        {
            var success = await _clusterService.ReplicateAsync(new DeleteIncidentCmd()
            {
                IncidentId = id
            });

            if (!success) return Problem();

            return Ok(SuccessResponse.FromSuccess);
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpPost("public/bulk")]
    [AllowAnonymous]
    public async Task<ActionResult<IncidentsForServicesRequest.Response>> GetIncidentsForAffectedServicesAsync([FromBody, Required] IncidentsForServicesRequest request)
    {
        try
        {
            var response = new IncidentsForServicesRequest.Response()
            {
                Incidents = new()
            };

            var searchResult = await _mediator.Send(new IncidentsQuery
            {
                Query = new(query => query
                    .Where(x => x.AffectedServices.Any(service => request.ServiceIds.Contains(service.Id)))
                    .Include(x => x.Timeline)
                    .Include(x => x.AffectedServices))
            });

            if (searchResult == null) return BadRequest();

            //Incident must have started on or after the request until timestamp, or else it would not be included anymore
            //Incindent must have continued to at least the start request timestamp (or is still running) to be included)
            var relevantResults = searchResult.Incidents
                .Where(x => x.From <= request.Until && (!x.Until.HasValue || request.From <= x.Until.Value))
                .ToList();

            //Return each incident as dto with pre-sorted timeline
            foreach (var incident in relevantResults)
            {
                incident.Timeline = incident.Timeline.OrderBy(x => x.DateTime).ToList();

                response.Incidents.Add(_mapper.Map<IncidentDto>(incident));
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem();
        }
    }

    public class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<IncidentTimelineItem, IncidentDto.IncidentTimelineItem>().ReverseMap();

            CreateMap<Incident, IncidentDto>().ReverseMap();

            CreateMap<List<string>, ICollection<MonitorBase>>().ConvertUsing(x => x.Select(y => new MonitorBase { Id = y }).ToList());

            CreateMap<ICollection<MonitorBase>, List<string>>().ConvertUsing(x => x.Select(y => y.Id).ToList());

            CreateMap<Incident, IncidentMetaDto>()
                .ForSourceMember(x => x.AffectedServices, opt => opt.DoNotValidate());
        }
    }
}
