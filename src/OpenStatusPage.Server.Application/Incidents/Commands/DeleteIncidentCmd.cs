using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Incidents.Commands
{
    public class DeleteIncidentCmd : MessageBase
    {
        public string IncidentId { get; set; }

        public class Handler : IRequestHandler<DeleteIncidentCmd>
        {
            private readonly IncidentService _incidentService;

            public Handler(IncidentService incidentService)
            {
                _incidentService = incidentService;
            }

            public async Task<Unit> Handle(DeleteIncidentCmd request, CancellationToken cancellationToken)
            {
                var incident = await _incidentService.Get(request.IncidentId).FirstOrDefaultAsync(cancellationToken);

                //Already deleted
                if (incident == null) return Unit.Value;

                await _incidentService.DeleteAsync(incident);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteIncidentCmd>
        {
            public Validator()
            {
                RuleFor(x => x.IncidentId)
                    .NotEmpty()
                    .WithMessage("Missing IncidentId");
            }
        }
    }
}
