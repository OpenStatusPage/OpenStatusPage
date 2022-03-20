using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Dns;
using OpenStatusPage.Server.Domain.Entities.Monitors.Http;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
using OpenStatusPage.Server.Domain.Entities.Monitors.Tcp;
using OpenStatusPage.Server.Domain.Entities.Monitors.Udp;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Shared.DataTransferObjects.Monitors;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Dns;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Http;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ping;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Ssh;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Tcp;
using OpenStatusPage.Shared.DataTransferObjects.Monitors.Udp;
using OpenStatusPage.Shared.Requests;

namespace OpenStatusPage.Server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MonitorsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly ClusterService _clusterService;

        public MonitorsController(IMapper mapper, IMediator mediator, ClusterService clusterService)
        {
            _mapper = mapper;
            _mediator = mediator;
            _clusterService = clusterService;
        }

        [HttpGet()]
        public async Task<ActionResult<List<MonitorMetaDto>>> GetAllAsync()
        {
            try
            {
                var results = new List<MonitorMetaDto>();

                var searchResult = await _mediator.Send(new MonitorsQuery());

                var monitors = searchResult?.Monitors;

                if (monitors != null)
                {
                    foreach (var monitor in monitors)
                    {
                        results.Add(new()
                        {
                            Id = monitor.Id,
                            Name = monitor.Name,
                            Type = monitor.GetRealEntityTypeString(),
                            Interval = monitor.Interval,
                            Enabled = monitor.Enabled,
                            Version = monitor.Version
                        });
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return Problem();
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MonitorDto>> GetDetailsAsync(string id)
        {
            try
            {
                var requestDtoType = await GetRequestDtoTypeAsync();

                if (requestDtoType == null) return BadRequest();

                var searchResult = await _mediator.Send(new MonitorsQuery
                {
                    Query = new(query => query
                        .Where(x => x.Id == id)
                        .Include(x => x.Rules)
                        .Include(x => x.NotificationProviders))
                });

                if (searchResult == null || searchResult.Monitors.Count > 1) return BadRequest();

                if (searchResult.Monitors.Count == 0) return NotFound();

                return Ok(await BuildBodyPolymorphAsync(searchResult.Monitors[0], requestDtoType));
            }
            catch (Exception ex)
            {
                return Problem();
            }
        }

        [HttpPost()]
        public async Task<ActionResult<SuccessResponse>> CreateOrUpdateAsync()
        {
            try
            {
                var data = await ReadBodyPolymorphAsync();

                var response = await _clusterService.ReplicateAsync(new CreateOrUpdateMonitorCmd()
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
        public async Task<ActionResult<SuccessResponse>> DeleteAsync(string id)
        {
            try
            {
                var response = await _clusterService.ReplicateAsync(new DeleteMonitorCmd()
                {
                    MonitorId = id
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
                CreateMap<MonitorBase, MonitorDto>().ReverseMap();

                CreateMap<DnsMonitor, DnsMonitorDto>().ReverseMap();
                CreateMap<DnsRecordRule, DnsRecordRuleDto>().ReverseMap();

                CreateMap<HttpMonitor, HttpMonitorDto>().ReverseMap();
                CreateMap<ResponseTimeRule, ResponseTimeRuleDto>().ReverseMap();
                CreateMap<ResponseBodyRule, ResponseBodyRuleDto>().ReverseMap();
                CreateMap<ResponseHeaderRule, ResponseHeaderRuleDto>().ReverseMap();
                CreateMap<SslCertificateRule, SslCertificateRuleDto>().ReverseMap();
                CreateMap<StatusCodeRule, StatusCodeRuleDto>().ReverseMap();

                CreateMap<PingMonitor, PingMonitorDto>().ReverseMap();

                CreateMap<SshMonitor, SshMonitorDto>().ReverseMap();
                CreateMap<SshCommandResultRule, SshCommandResultRuleDto>().ReverseMap();

                CreateMap<TcpMonitor, TcpMonitorDto>().ReverseMap();

                CreateMap<UdpMonitor, UdpMonitorDto>().ReverseMap();
                CreateMap<ResponseBytesRule, ResponseBytesRuleDto>().ReverseMap();

                CreateMap<string, byte[]>().ConvertUsing(str => ParseBytesFromString(str));
                CreateMap<byte[], string>().ConvertUsing(bytes => BytesAsString(bytes));
            }
        }

        protected async Task<MonitorDto> BuildBodyPolymorphAsync(MonitorBase monitor, Type requestDtoType)
        {
            var resultDto = _mapper.Map(monitor, monitor.GetRealEntityType(), requestDtoType) as MonitorDto;

            //Assign notifaction provider colletion
            resultDto.NotificationProviderMetas = new();
            foreach (var notificationProvider in monitor.NotificationProviders)
            {
                resultDto.NotificationProviderMetas.Add(new()
                {
                    Id = notificationProvider.Id,
                    Name = notificationProvider.Name,
                    Type = notificationProvider.GetRealEntityTypeString(),
                });
            }

            //Assign rule collections based on type
            if (monitor.Rules != null && monitor.Rules.Count > 0)
            {
                switch (resultDto)
                {
                    case DnsMonitorDto dnsMonitorDto:
                    {
                        dnsMonitorDto.DnsRecordRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is DnsRecordRule).Select(x => x as DnsRecordRule), dnsMonitorDto.DnsRecordRules);
                        break;
                    }

                    case HttpMonitorDto httpMonitorDto:
                    {
                        httpMonitorDto.ResponseTimeRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseTimeRule).Select(x => x as ResponseTimeRule), httpMonitorDto.ResponseTimeRules);

                        httpMonitorDto.ResponseBodyRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseBodyRule).Select(x => x as ResponseBodyRule), httpMonitorDto.ResponseBodyRules);

                        httpMonitorDto.ResponseHeaderRules = new();
                        httpMonitorDto.ResponseHeaderRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseHeaderRule).Select(x => x as ResponseHeaderRule), httpMonitorDto.ResponseHeaderRules);

                        httpMonitorDto.SslCertificateRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is SslCertificateRule).Select(x => x as SslCertificateRule), httpMonitorDto.SslCertificateRules);

                        httpMonitorDto.StatusCodeRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is StatusCodeRule).Select(x => x as StatusCodeRule), httpMonitorDto.StatusCodeRules);

                        break;
                    }

                    case PingMonitorDto pingMonitorDto:
                    {
                        pingMonitorDto.ResponseTimeRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseTimeRule).Select(x => x as ResponseTimeRule), pingMonitorDto.ResponseTimeRules);
                        break;
                    }

                    case SshMonitorDto sshMonitorDto:
                    {
                        sshMonitorDto.CommandResultRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is SshCommandResultRule).Select(x => x as SshCommandResultRule), sshMonitorDto.CommandResultRules);
                        break;
                    }

                    case TcpMonitorDto tcpMonitorDto:
                    {
                        tcpMonitorDto.ResponseTimeRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseTimeRule).Select(x => x as ResponseTimeRule), tcpMonitorDto.ResponseTimeRules);
                        break;
                    }

                    case UdpMonitorDto udpMonitorDto:
                    {
                        udpMonitorDto.ResponseTimeRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseTimeRule).Select(x => x as ResponseTimeRule), udpMonitorDto.ResponseTimeRules);

                        udpMonitorDto.ResponseBytesRules = new();
                        _mapper.Map(monitor.Rules.Where(x => x is ResponseBytesRule).Select(x => x as ResponseBytesRule), udpMonitorDto.ResponseBytesRules);
                        break;
                    }
                }
            }


            return resultDto;
        }

        protected async Task<MonitorBase> ReadBodyPolymorphAsync()
        {
            var dtoType = await GetRequestDtoTypeAsync();

            if (dtoType == null) throw new ArgumentNullException("type");

            var dto = await Request.ReadFromJsonAsync(dtoType) as MonitorDto;

            var entityType = dtoType.Name.ToLowerInvariant() switch
            {
                "dnsmonitordto" => typeof(DnsMonitor),
                "httpmonitordto" => typeof(HttpMonitor),
                "pingmonitordto" => typeof(PingMonitor),
                "sshmonitordto" => typeof(SshMonitor),
                "tcpmonitordto" => typeof(TcpMonitor),
                "udpmonitordto" => typeof(UdpMonitor),
                _ => typeof(MonitorBase)
            };

            var container = _mapper.Map(dto, dtoType, entityType) as MonitorBase;

            //Select the assigned notification providers (we just need their ids for the command to do the rest)
            if (dto.NotificationProviderMetas != null && dto.NotificationProviderMetas.Count > 0)
            {
                container.NotificationProviders = dto.NotificationProviderMetas.Select(x => new NotificationProvider
                {
                    Id = x.Id,
                }).ToList();
            }

            //Read the polymorph rules back into one collection
            var rules = new List<MonitorRule>();

            switch (dto)
            {
                case DnsMonitorDto dnsMonitorDto:
                {
                    rules.AddRange(_mapper.Map<List<DnsRecordRule>>(dnsMonitorDto.DnsRecordRules));
                    break;
                }

                case HttpMonitorDto httpMonitorDto:
                {
                    rules.AddRange(_mapper.Map<List<ResponseTimeRule>>(httpMonitorDto.ResponseTimeRules));

                    rules.AddRange(_mapper.Map<List<ResponseBodyRule>>(httpMonitorDto.ResponseBodyRules));

                    rules.AddRange(_mapper.Map<List<ResponseHeaderRule>>(httpMonitorDto.ResponseHeaderRules));

                    rules.AddRange(_mapper.Map<List<SslCertificateRule>>(httpMonitorDto.SslCertificateRules));

                    rules.AddRange(_mapper.Map<List<StatusCodeRule>>(httpMonitorDto.StatusCodeRules));
                    break;
                }

                case PingMonitorDto pingMonitorDto:
                {
                    rules.AddRange(_mapper.Map<List<ResponseTimeRule>>(pingMonitorDto.ResponseTimeRules));
                    break;
                }

                case SshMonitorDto sshMonitorDto:
                {
                    rules.AddRange(_mapper.Map<List<SshCommandResultRule>>(sshMonitorDto.CommandResultRules));
                    break;
                }

                case TcpMonitorDto tcpMonitorDto:
                {
                    rules.AddRange(_mapper.Map<List<ResponseTimeRule>>(tcpMonitorDto.ResponseTimeRules));
                    break;
                }

                case UdpMonitorDto udpMonitorDto:
                {
                    rules.AddRange(_mapper.Map<List<ResponseTimeRule>>(udpMonitorDto.ResponseTimeRules));

                    rules.AddRange(_mapper.Map<List<ResponseBytesRule>>(udpMonitorDto.ResponseBytesRules));
                    break;
                }
            }

            container.Rules = rules;

            return container;
        }

        protected async Task<Type> GetRequestDtoTypeAsync()
        {
            var typeName = Request.Query.FirstOrDefault(x => x.Key.ToLowerInvariant() == "typename").Value.FirstOrDefault()?.ToLowerInvariant();

            var dtoType = typeName switch
            {
                "dnsmonitordto" => typeof(DnsMonitorDto),
                "httpmonitordto" => typeof(HttpMonitorDto),
                "pingmonitordto" => typeof(PingMonitorDto),
                "sshmonitordto" => typeof(SshMonitorDto),
                "tcpmonitordto" => typeof(TcpMonitorDto),
                "udpmonitordto" => typeof(UdpMonitorDto),
                _ => typeof(MonitorDto)
            };

            return dtoType;
        }

        protected static byte[] ParseBytesFromString(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return Array.Empty<byte>();

            var hexValues = data.Trim();

            while (hexValues.Contains("  "))
            {
                hexValues = hexValues.Replace("  ", " ");
            }

            var hexSplit = hexValues.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var bytes = new List<byte>(hexSplit.Length);

            foreach (string hex in hexSplit)
            {
                bytes.Add(Convert.ToByte(hex, 16));
            }

            return bytes.ToArray();
        }

        protected static string BytesAsString(byte[] bytes)
        {
            return string.Join(" ", bytes.Select(x => $"{x:X2}"));
        }
    }
}
