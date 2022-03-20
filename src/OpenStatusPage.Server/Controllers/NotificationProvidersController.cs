using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Application.Notifications.Providers.Commands;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Shared.DataTransferObjects.NotificationProviders;
using OpenStatusPage.Shared.Requests;

namespace OpenStatusPage.Server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class NotificationProvidersController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly ClusterService _clusterService;

        public NotificationProvidersController(IMapper mapper, IMediator mediator, ClusterService clusterService)
        {
            _mapper = mapper;
            _mediator = mediator;
            _clusterService = clusterService;
        }

        [HttpGet()]
        public async Task<ActionResult<List<NotificationProviderMetaDto>>> GetAllAsync()
        {
            try
            {
                var results = new List<NotificationProviderMetaDto>();

                var searchResult = await _mediator.Send(new NotificationProvidersQuery());

                var notificationProviders = searchResult?.NotificationProviders;

                if (notificationProviders != null)
                {
                    foreach (var notificationProvider in notificationProviders)
                    {
                        results.Add(new()
                        {
                            Id = notificationProvider.Id,
                            Name = notificationProvider.Name,
                            Type = notificationProvider.GetRealEntityTypeString(),
                            DefaultForNewMonitors = notificationProvider.DefaultForNewMonitors,
                        });
                    }
                }

                return Ok(results);
            }
            catch (Exception)
            {
                return Problem();
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationProviderDto>> GetProviderDetailsAsync(string id)
        {
            try
            {
                var requestDtoType = await GetRequestDtoTypeAsync();

                var searchResult = await _mediator.Send(new NotificationProvidersQuery
                {
                    Query = new(query => query.Where(x => x.Id == id))
                });

                if (searchResult == null || searchResult.NotificationProviders.Count > 1) return BadRequest();

                if (searchResult.NotificationProviders.Count == 0) return NotFound();

                var provider = searchResult.NotificationProviders[0];

                return Ok(_mapper.Map(provider, provider.GetRealEntityType(), requestDtoType));
            }
            catch (Exception)
            {
                return Problem();
            }
        }

        [HttpPost()]
        public async Task<ActionResult<SuccessResponse>> CreaterOrUpdateProviderAsync()
        {
            try
            {
                var data = await ReadBodyPolymorphAsync();

                var response = await _clusterService.ReplicateAsync(new CreateOrUpdateNotificationProviderCmd()
                {
                    Data = data
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
        public async Task<ActionResult<SuccessResponse>> DeleteProviderAsync(string id)
        {
            try
            {
                var response = await _clusterService.ReplicateAsync(new DeleteNotificationProviderCmd()
                {
                    ProviderId = id
                });

                if (!response) return Problem();

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
                CreateMap<NotificationProvider, NotificationProviderDto>().ReverseMap();
                CreateMap<WebhookProvider, WebhookProviderDto>().ReverseMap();
                CreateMap<SmtpEmailProvider, SmtpEmailProviderDto>().ReverseMap();
            }
        }

        protected async Task<NotificationProvider> ReadBodyPolymorphAsync()
        {
            var dtoType = await GetRequestDtoTypeAsync();

            if (dtoType == null) throw new ArgumentNullException("type");

            var dto = await Request.ReadFromJsonAsync(dtoType);

            var entityType = dtoType.Name.ToLowerInvariant() switch
            {
                "webhookproviderdto" => typeof(WebhookProvider),
                "smtpemailproviderdto" => typeof(SmtpEmailProvider),
                _ => typeof(NotificationProvider)
            };

            return _mapper.Map(dto, dtoType, entityType) as NotificationProvider;
        }

        protected async Task<Type> GetRequestDtoTypeAsync()
        {
            var typeName = Request.Query.FirstOrDefault(x => x.Key.ToLowerInvariant() == "typename").Value.FirstOrDefault()?.ToLowerInvariant();

            var dtoType = typeName switch
            {
                "webhookproviderdto" => typeof(WebhookProviderDto),
                "smtpemailproviderdto" => typeof(SmtpEmailProviderDto),
                _ => typeof(NotificationProviderDto)
            };

            return dtoType;
        }
    }
}
