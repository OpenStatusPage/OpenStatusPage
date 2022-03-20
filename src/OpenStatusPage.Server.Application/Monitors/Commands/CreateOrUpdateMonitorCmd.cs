using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc.Attributes;
using OpenStatusPage.Server.Application.Notifications.Providers;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Dns;
using OpenStatusPage.Server.Domain.Entities.Monitors.Http;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
using OpenStatusPage.Server.Domain.Entities.Monitors.Tcp;
using OpenStatusPage.Server.Domain.Entities.Monitors.Udp;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.Monitors.Commands
{
    [RequiresDbTransaction]
    public class CreateOrUpdateMonitorCmd : MessageBase
    {
        public MonitorBase Data { get; set; }

        public class Handler : IRequestHandler<CreateOrUpdateMonitorCmd>
        {
            private readonly MonitorService _monitorService;
            private readonly NotificationProviderService _notificationProviderService;
            private readonly IMapper _mapper;

            public Handler(MonitorService monitorService, NotificationProviderService notificationProviderService, IMapper mapper)
            {
                _monitorService = monitorService;
                _notificationProviderService = notificationProviderService;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(CreateOrUpdateMonitorCmd request, CancellationToken cancellationToken)
            {
                var monitors = await _monitorService.Get()
                    .Where(x => x.Id == request.Data.Id || x.Name.ToLower().Equals(request.Data.Name.ToLower()))
                    .Include(x => x.Rules)
                    .Include(x => x.NotificationProviders)
                    .ToListAsync(cancellationToken);

                if (monitors.Any(x => x.Id != request.Data.Id)) throw new ValidationException("Another monitor with the same name already exists");

                var monitor = monitors.FirstOrDefault();

                //Only apply the command if the entity is not already updated (can happen if multiple members share the same db)
                if (monitor != null && monitor.Version >= request.Data.Version) return Unit.Value;

                var notificationProviders = new List<NotificationProvider>();

                //Replace dummy notification providers with tracked entities
                if (request.Data.NotificationProviders != null && request.Data.NotificationProviders.Count > 0)
                {
                    var providerIds = request.Data.NotificationProviders.Select(x => x.Id).ToList();

                    notificationProviders = await _notificationProviderService.Get()
                            .Where(x => providerIds.Contains(x.Id))
                            .ToListAsync(cancellationToken);
                }

                if (monitor == null) //Handle creation
                {
                    monitor = await (await _monitorService.CreateAsync(request.Data)).FirstOrDefaultAsync(cancellationToken);
                }
                else //Handle update
                {
                    //Apply new values from data container
                    _mapper.Map(request.Data, monitor);
                }

                if (monitor == null) throw new Exception("Could not create or update the monitor.");

                //Reapply polymorph collections because automapper can not know the concrete instances
                monitor.NotificationProviders = notificationProviders;

                //Create empty rule set if it does not exist yet
                monitor.Rules ??= new List<MonitorRule>();

                //Remove tracked rules that are not in the request rules anymore
                monitor.Rules
                    .Where(x => !request.Data.Rules.Any(y => y.Id == x.Id))
                    .ToList()
                    .ForEach(x => monitor.Rules.Remove(x));

                //Add rules that did not exist yet
                request.Data.Rules
                    .Where(x => !monitor.Rules.Any(y => y.Id == x.Id))
                    .ToList()
                    .ForEach(x =>
                    {
                        if (_mapper.Map(x, x.GetType(), x.GetType()) is MonitorRule monitorRule)
                        {
                            monitor.Rules.Add(monitorRule);
                        }
                    });

                //Save changes
                await _monitorService.UpdateAsync(monitor);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateOrUpdateMonitorCmd>
        {
            public Validator()
            {
                RuleFor(x => x.Data)
                    .NotNull()
                    .WithMessage("Missing data object.")
                    .DependentRules(() =>
                    {
                        RuleFor(x => x.Data).SetInheritanceValidator(v =>
                        {
                            v.Add(new MonitorBaseValidator<MonitorBase>());
                            v.Add(new DnsMonitorValidator());
                            v.Add(new HttpMonitorValidator());
                            v.Add(new PingMonitorValidator());
                            v.Add(new SshMonitorValidator());
                            v.Add(new TcpMonitorValidator());
                            v.Add(new UdpMonitorValidator());
                        });
                    });
            }
        }

        public class MonitorBaseValidator<T> : AbstractValidator<T> where T : MonitorBase
        {
            public MonitorBaseValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Field Id is required.");

                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Field Name is required.");

                RuleFor(x => x.Interval)
                    .NotNull()
                    .WithMessage("Field Interval is required.")
                    .GreaterThan(TimeSpan.Zero)
                    .WithMessage("Invalid value for field Interval");

                RuleFor(x => x.RetryInterval)
                    .NotNull()
                    .WithMessage("Field Interval is required.")
                    .GreaterThan(TimeSpan.Zero)
                    .WithMessage("Invalid value for field Interval")
                    .When(x => x.Retries.HasValue);

                RuleForEach(x => x.Rules)
                    .Must(y => !string.IsNullOrEmpty(y.Id))
                    .WithMessage("All rules must have an Id")
                    .Must(y => !string.IsNullOrEmpty(y.MonitorId))
                    .WithMessage("All rules must have an MonitorId");

                RuleForEach(x => x.NotificationProviders)
                    .Must(y => !string.IsNullOrEmpty(y.Id))
                    .WithMessage("All associated incidents must have an Id");

                RuleFor(x => x.WorkerCount)
                    .GreaterThan(0)
                    .WithMessage("WorkerCount must be greater than 0.");
            }
        }

        public class DnsMonitorValidator : MonitorBaseValidator<DnsMonitor>
        {
            public DnsMonitorValidator()
            {
                RuleFor(x => x.Hostname)
                    .NotEmpty()
                    .WithMessage("Field Hostname is required.");
            }
        }

        public class HttpMonitorValidator : MonitorBaseValidator<HttpMonitor>
        {
            public HttpMonitorValidator()
            {
                RuleFor(x => x.Url)
                    .NotEmpty()
                    .WithMessage("Field Url is required.")
                    .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                    .WithMessage("Field Url value is not valid.");

                RuleFor(x => x.AuthenticationBase)
                    .NotEmpty()
                    .WithMessage("Field AuthenticationBase is required.")
                    .When(x => x.AuthenticationScheme != HttpAuthenticationScheme.None);

                RuleFor(x => x.AuthenticationAdditional)
                    .NotEmpty()
                    .WithMessage("Field AuthenticationAdditional is required.")
                    .When(x => x.AuthenticationScheme == HttpAuthenticationScheme.Basic ||
                          x.AuthenticationScheme == HttpAuthenticationScheme.Digest);
            }
        }

        public class PingMonitorValidator : MonitorBaseValidator<PingMonitor>
        {
            public PingMonitorValidator()
            {
                RuleFor(x => x.Hostname)
                    .NotEmpty()
                    .WithMessage("Field Hostname is required.");
            }
        }

        public class SshMonitorValidator : MonitorBaseValidator<SshMonitor>
        {
            public SshMonitorValidator()
            {
                RuleFor(x => x.Hostname)
                    .NotEmpty()
                    .WithMessage("Field Hostname is required.");

                RuleFor(x => x.Username)
                    .NotEmpty()
                    .WithMessage("Field Username is required.");
            }
        }

        public class TcpMonitorValidator : MonitorBaseValidator<TcpMonitor>
        {
            public TcpMonitorValidator()
            {
                RuleFor(x => x.Hostname)
                    .NotEmpty()
                    .WithMessage("Field Hostname is required.");

                RuleFor(x => x.Port)
                    .NotEmpty()
                    .WithMessage("Field Port is required.");
            }
        }

        public class UdpMonitorValidator : MonitorBaseValidator<UdpMonitor>
        {
            public UdpMonitorValidator()
            {
                RuleFor(x => x.Hostname)
                    .NotEmpty()
                    .WithMessage("Field Hostname is required.");

                RuleFor(x => x.Port)
                    .NotEmpty()
                    .WithMessage("Field Port is required.");

                RuleFor(x => x.RequestBytes)
                    .NotNull()
                    .WithMessage("Field RequestBytes is required.");
            }
        }
    }
}
