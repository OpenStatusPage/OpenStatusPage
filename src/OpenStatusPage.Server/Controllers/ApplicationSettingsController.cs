using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Domain.Entities.Configuration;
using OpenStatusPage.Shared.DataTransferObjects.Configuration;
using OpenStatusPage.Shared.Requests;

namespace OpenStatusPage.Server.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ApplicationSettingsController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ClusterService _clusterService;

    public ApplicationSettingsController(IMapper mapper, IMediator mediator, ClusterService clusterService)
    {
        _mapper = mapper;
        _mediator = mediator;
        _clusterService = clusterService;
    }

    [HttpGet()]
    public async Task<ActionResult<ApplicationSettingsDto>> GetResultAsync()
    {
        try
        {
            var queryResult = await _mediator.Send(new ApplicationSettingsQuery());

            if (queryResult == null) return NotFound();

            return Ok(_mapper.Map<ApplicationSettingsDto>(queryResult.ApplicationSettings));
        }
        catch (Exception ex)
        {
            return Problem();
        }
    }

    [HttpPost()]
    public async Task<ActionResult<SuccessResponse>> PostDataAsync([FromBody] ApplicationSettingsDto applicationSettings)
    {
        try
        {
            var data = _mapper.Map<ApplicationSettings>(applicationSettings);

            var success = await _clusterService.ReplicateAsync(new CreateOrUpdateApplicationSettingsCmd()
            {
                Data = data
            });

            if (!success) return Problem();

            return Ok(SuccessResponse.FromSuccess);
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
            CreateMap<ApplicationSettings, ApplicationSettingsDto>()
                .ReverseMap();
        }
    }
}
