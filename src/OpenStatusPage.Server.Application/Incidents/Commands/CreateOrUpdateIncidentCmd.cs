using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc.Attributes;
using OpenStatusPage.Server.Application.Monitors;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Domain.Entities.Monitors;

namespace OpenStatusPage.Server.Application.Incidents.Commands
{
    [RequiresDbTransaction]
    public class CreateOrUpdateIncidentCmd : MessageBase
    {
        public Incident Data { get; set; }

        public class Handler : IRequestHandler<CreateOrUpdateIncidentCmd>
        {
            private readonly IncidentService _incidentService;
            private readonly MonitorService _monitorService;

            public Handler(IncidentService incidentService, MonitorService monitorService)
            {
                _incidentService = incidentService;
                _monitorService = monitorService;
            }

            public async Task<Unit> Handle(CreateOrUpdateIncidentCmd request, CancellationToken cancellationToken)
            {
                var incident = await _incidentService
                        .Get(request.Data.Id)
                        .Include(x => x.AffectedServices)
                        .Include(x => x.Timeline)
                        .FirstOrDefaultAsync(cancellationToken);

                //Only apply the command if the entity is not already updated (can happen if multiple members share the same db)
                if (incident != null && incident.Version >= request.Data.Version) return Unit.Value;

                var affectedServices = new List<MonitorBase>();

                //Replace dummy monitors with tracked entities
                if (request.Data.AffectedServices != null && request.Data.AffectedServices.Count > 0)
                {
                    var monitorIds = request.Data.AffectedServices.Select(x => x.Id).ToList();

                    affectedServices = await _monitorService.Get()
                        .Where(x => monitorIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);
                }

                if (incident == null) //Handle creation
                {
                    incident = await (await _incidentService.CreateAsync(request.Data)).FirstOrDefaultAsync(cancellationToken);
                }
                else //Handle update
                {
                    //Apply latest version
                    incident.Version = request.Data.Version;

                    //Apply new values from data container
                    incident.Name = request.Data.Name;
                    incident.From = request.Data.From;
                    incident.Until = request.Data.Until;
                    incident.Timeline = request.Data.Timeline;
                }

                if (incident == null) throw new Exception("Could not create or update the incident.");

                //Reapply polymorph collections because automapper can not know the concrete instances
                incident.AffectedServices = affectedServices;

                //Save the changes
                await _incidentService.UpdateAsync(incident);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateOrUpdateIncidentCmd>
        {
            public Validator()
            {
                RuleFor(x => x.Data)
                    .NotNull()
                    .WithMessage("Missing data object.")
                    .DependentRules(() =>
                    {
                        RuleFor(x => x.Data.Id)
                            .NotEmpty()
                            .WithMessage("Field Id is required.");

                        RuleFor(x => x.Data.Name)
                            .NotEmpty()
                            .WithMessage("Field Name is required.");

                        RuleFor(x => x.Data.From)
                            .NotNull()
                            .WithMessage("Field From is required.");

                        RuleFor(x => x.Data.Timeline)
                            .NotEmpty()
                            .WithMessage("Timeline must contain at least one item.");

                        RuleForEach(x => x.Data.Timeline)
                            .Must(y => !string.IsNullOrEmpty(y.Id))
                            .WithMessage("All timeline items must have an Id");
                    });
            }
        }
    }
}
