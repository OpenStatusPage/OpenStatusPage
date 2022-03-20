using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.StatusPages.Commands;
using OpenStatusPage.Server.Domain.Entities.StatusPages;
using OpenStatusPage.Shared.DataTransferObjects.StatusPages;
using OpenStatusPage.Shared.Requests;
using OpenStatusPage.Shared.Utilities;

namespace OpenStatusPage.Server.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class StatusPagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ClusterService _clusterService;

    public StatusPagesController(IMediator mediator, IMapper mapper, ClusterService clusterService)
    {
        _mediator = mediator;
        _mapper = mapper;
        _clusterService = clusterService;
    }

    [HttpGet()]
    public async Task<ActionResult<List<StatusPageMetaDto>>> GetAllAsync()
    {
        try
        {
            var searchResult = await _mediator.Send(new StatusPagesQuery());

            var statuspages = searchResult?.StatusPages;

            if (statuspages == null) Problem();

            return Ok(_mapper.Map<List<StatusPageMetaDto>>(statuspages));
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StatusPageConfigurationDto>> GetDetailsAsync(string id)
    {
        try
        {
            var searchResult = await _mediator.Send(new StatusPagesQuery
            {
                Query = new(query => query
                    .Where(x => x.Id == id)
                    .Include(x => x.MonitorSummaries)
                    .ThenInclude(x => x.LabeledMonitors))
            });

            if (searchResult == null || searchResult.StatusPages.Count > 1) return BadRequest();

            if (searchResult.StatusPages.Count == 0) return NotFound();

            return Ok(_mapper.Map<StatusPageConfigurationDto>(searchResult.StatusPages[0]));
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
            var data = await Request.ReadFromJsonAsync<StatusPageConfigurationDto>();

            var statusPageData = _mapper.Map<StatusPage>(data);

            var response = await _clusterService.ReplicateAsync(new CreateOrUpdateStatusPageCmd()
            {
                Data = statusPageData
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
            var appSettings = (await _mediator.Send(new ApplicationSettingsQuery()))?.ApplicationSettings;

            if (appSettings != null && appSettings.DefaultStatusPageId == id) return BadRequest();

            var response = await _clusterService.ReplicateAsync(new DeleteStatusPageCmd()
            {
                StatusPageId = id
            });

            if (!response) return Problem();

            return Ok(SuccessResponse.FromSuccess);
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("public/{searchBy?}")]
    [AllowAnonymous]
    public async Task<ActionResult<StatusPageDto>> GetStatusPagePublicAsync(string? searchBy, [FromHeader(Name = "X-StatusPage-Access-Token")] string? accesstoken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchBy)) return BadRequest();

            if (searchBy == "default")
            {
                var appSettings = await _mediator.Send(new ApplicationSettingsQuery());

                if (appSettings == null || appSettings.ApplicationSettings == null) return BadRequest();

                searchBy = appSettings.ApplicationSettings.DefaultStatusPageId;

                if (string.IsNullOrEmpty(searchBy)) return NotFound();
            }

            var searchResult = await _mediator.Send(new StatusPagesQuery
            {
                Query = new(query => query
                    .Where(x => x.Id == searchBy || x.Name.ToLower().Equals(searchBy.ToLower()))
                    .Include(x => x.MonitorSummaries)
                    .ThenInclude(x => x.LabeledMonitors))
            });

            if (searchResult == null || searchResult.StatusPages.Count != 1) return NotFound();

            var statusPage = searchResult.StatusPages[0];

            if (!string.IsNullOrWhiteSpace(statusPage.Password) && accesstoken != SHA256Hash.Create(statusPage.Password))
            {
                return Unauthorized();
            }

            return Ok(_mapper.Map<StatusPageDto>(statusPage));
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    public class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<LabeledMonitor, StatusPageDto.MonitorSummary.LabeledMonitor>().ReverseMap();

            CreateMap<MonitorSummary, StatusPageDto.MonitorSummary>().ReverseMap();

            CreateMap<StatusPage, StatusPageConfigurationDto>().ReverseMap();

            //Do not allow password hash to be shared in fully data
            CreateMap<StatusPage, StatusPageDto>()
                .ForSourceMember(x => x.Password, opt => opt.DoNotValidate())
                .ReverseMap();

            //Do not allow password hash to be shared in meta data
            CreateMap<StatusPage, StatusPageMetaDto>()
                .ForMember(x => x.IsPublic, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Password)))
                .ForSourceMember(x => x.Password, opt => opt.DoNotValidate())
                .ReverseMap();
        }
    }
}
