using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Shared.DataTransferObjects.Cluster;
using OpenStatusPage.Shared.Requests;

namespace OpenStatusPage.Server.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ClusterMembersController : ControllerBase
{
    private readonly ClusterService _clusterService;
    private readonly IMapper _mapper;

    public ClusterMembersController(ClusterService clusterService, IMapper mapper)
    {
        _clusterService = clusterService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClusterMemberDto>>> GetAllAsync()
    {
        return Ok(_mapper.Map<List<ClusterMemberDto>>(await _clusterService.GetMembersAsync(true)));
    }

    [HttpGet("tags")]
    public async Task<ActionResult<List<string>>> GetAllTagsAsync()
    {
        var members = await _clusterService.GetMembersAsync(true);

        return Ok(members.SelectMany(x => x.Tags).Distinct().ToList());
    }

    [HttpDelete("{id?}")]
    public async Task<ActionResult<SuccessResponse>> DeleteAsync(string? id, [FromBody] Uri endpoint)
    {
        try
        {
            var member = await _clusterService.GetMemberByEndpointAsync(endpoint);

            if (member == null) return NotFound();

            if (!await _clusterService.RemoveMemberAsync(member)) Problem();

            return Ok(SuccessResponse.FromSuccess);
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("public/endpoints")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Uri>>> GetEndpointsAsync()
    {
        return Ok((await _clusterService.GetMembersAsync()).Select(x => x.Endpoint).ToList());
    }

    public class DtoMapper : Profile
    {
        public DtoMapper()
        {
            CreateMap<ClusterMember, ClusterMemberDto>().ReverseMap();
        }
    }
}
