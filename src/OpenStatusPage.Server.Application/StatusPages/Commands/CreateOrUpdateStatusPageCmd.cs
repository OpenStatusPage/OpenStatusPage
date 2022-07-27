using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Domain.Entities.StatusPages;

namespace OpenStatusPage.Server.Application.StatusPages.Commands
{
    public class CreateOrUpdateStatusPageCmd : MessageBase
    {
        public StatusPage Data { get; set; }

        public class Handler : IRequestHandler<CreateOrUpdateStatusPageCmd>
        {
            private readonly StatusPageService _statusPageService;
            private readonly IMapper _mapper;

            public Handler(StatusPageService statusPageService, IMapper mapper)
            {
                _statusPageService = statusPageService;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(CreateOrUpdateStatusPageCmd request, CancellationToken cancellationToken)
            {
                var statusPages = await _statusPageService.Get()
                    .Where(x => x.Id == request.Data.Id || x.Name.ToLower().Equals(request.Data.Name.ToLower()))
                    .Include(x => x.MonitorSummaries)
                    .ThenInclude(x => x.LabeledMonitors)
                    .ToListAsync(cancellationToken);

                if (statusPages.Any(x => x.Id != request.Data.Id)) throw new ValidationException("Another status page with the same name already exists");

                var statusPage = statusPages.FirstOrDefault();

                //Only apply the command if the entity is not already updated (can happen if multiple members share the same db)
                if (statusPage != null && statusPage.Version >= request.Data.Version) return Unit.Value;

                if (statusPage == null) //Handle creation
                {
                    statusPage = await (await _statusPageService.CreateAsync(request.Data)).FirstOrDefaultAsync(cancellationToken);

                    if (statusPage == null) throw new Exception("Could not create the status page.");
                }
                else //Handle update
                {
                    //Apply new values from data container
                    _mapper.Map(request.Data, statusPage);

                    //Save changes
                    await _statusPageService.UpdateAsync(statusPage);
                }

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateOrUpdateStatusPageCmd>
        {
            public Validator()
            {
                RuleFor(x => x.Data)
                    .NotNull()
                    .WithMessage("Missing data object.").DependentRules(() =>
                    {
                        RuleFor(x => x.Data.Id)
                            .NotEmpty()
                            .WithMessage("Field Id is required.");

                        RuleFor(x => x.Data.Name)
                            .NotEmpty()
                                .WithMessage("Field Name is required.")
                            .Must(x => x.All(c => char.IsLetterOrDigit(c) || c == '-'))
                                .WithMessage("Only values a-zA-Z0-9 and '-' are allowed for field Name.")
                            .Must(x => x.ToLowerInvariant() != "default")
                                .WithMessage("Value 'default' for field Name is reserved.")
                            .Must(x => x.ToLowerInvariant() != "dashboard")
                                .WithMessage("Value 'dashboard' for field Name is reserved.");

                        RuleFor(x => x.Data.DaysUpcomingMaintenances)
                            .NotNull()
                                .WithMessage("Field DaysUpcomingMaintenances is required.")
                                .DependentRules(() =>
                                {
                                    RuleFor(x => x.Data.DaysUpcomingMaintenances)
                                        .InclusiveBetween(1, 90)
                                        .WithMessage("Invalid value for field DaysUpcomingMaintenances. Allowed values 1-90");
                                })
                            .When(x => x.Data.EnableUpcomingMaintenances);

                        RuleFor(x => x.Data.DaysIncidentTimeline)
                            .NotNull()
                                .WithMessage("Field DaysIncidentTimeline is required.")
                                .DependentRules(() =>
                                {
                                    RuleFor(x => x.Data.DaysUpcomingMaintenances)
                                        .InclusiveBetween(1, 90)
                                        .WithMessage("Invalid value for field DaysUpcomingMaintenances. Allowed values 1-90");
                                })
                            .When(x => x.Data.EnableIncidentTimeline);

                        RuleForEach(x => x.Data.MonitorSummaries)
                            .SetValidator(new MonitorSummaryValidator());
                    });
            }
        }

        public class MonitorSummaryValidator : AbstractValidator<MonitorSummary>
        {
            public MonitorSummaryValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Field Id is required.");

                RuleFor(x => x.StatusPageId)
                    .NotEmpty()
                    .WithMessage("Field StatusPageId is required.");

                RuleFor(x => x.Title)
                    .NotEmpty()
                    .WithMessage("Field Title is required.");

                RuleForEach(x => x.LabeledMonitors)
                    .SetValidator(new LabeledMonitorValidator());
            }
        }

        public class LabeledMonitorValidator : AbstractValidator<LabeledMonitor>
        {
            public LabeledMonitorValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Field Id is required.");

                RuleFor(x => x.MonitorSummaryId)
                    .NotEmpty()
                    .WithMessage("Field MonitorSummaryId is required.");

                RuleFor(x => x.MonitorId)
                    .NotEmpty()
                    .WithMessage("Field MonitorId is required.");

                RuleFor(x => x.Label)
                    .NotEmpty()
                    .WithMessage("Field Label is required.");
            }
        }
    }
}
