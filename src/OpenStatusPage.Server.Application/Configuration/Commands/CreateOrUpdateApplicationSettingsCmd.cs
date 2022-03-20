using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc.Attributes;
using OpenStatusPage.Server.Domain.Entities.Configuration;

namespace OpenStatusPage.Server.Application.Configuration.Commands
{
    [RequiresDbTransaction]
    public class CreateOrUpdateApplicationSettingsCmd : MessageBase
    {
        public ApplicationSettings Data { get; set; }

        public class Handler : IRequestHandler<CreateOrUpdateApplicationSettingsCmd>
        {
            private readonly ApplicationSettingsService _applicationSettingsService;
            private readonly IMapper _mapper;

            public Handler(ApplicationSettingsService applicationSettingsService, IMapper mapper)
            {
                _applicationSettingsService = applicationSettingsService;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(CreateOrUpdateApplicationSettingsCmd request, CancellationToken cancellationToken)
            {
                var applicationSettings = await _applicationSettingsService.Get().FirstOrDefaultAsync(cancellationToken);

                //Only apply the command if the entity is not already updated (can happen if multiple members share the same db)
                if (applicationSettings != null && applicationSettings.Version >= request.Data.Version) return Unit.Value;

                if (applicationSettings == null) //Handle creation
                {
                    applicationSettings = await (await _applicationSettingsService.CreateAsync(request.Data)).FirstOrDefaultAsync(cancellationToken);

                    if (applicationSettings == null) throw new Exception("Could not create application settings.");
                }
                else //Handle update
                {
                    //Apply new values from data container
                    _mapper.Map(request.Data, applicationSettings);

                    //Save changes
                    await _applicationSettingsService.UpdateAsync(applicationSettings);
                }

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateOrUpdateApplicationSettingsCmd>
        {
            public Validator()
            {
                RuleFor(x => x.Data)
                    .NotNull()
                    .WithMessage("Missing data transfer object.")
                    .DependentRules(() =>
                    {
                        RuleFor(x => x.Data.DefaultStatusPageId)
                            .NotEmpty()
                            .WithMessage("Missing DefaultStatusPageId.");

                        RuleFor(x => x.Data.DaysMonitorHistory)
                            .InclusiveBetween((ushort)0, (ushort)90)
                            .WithMessage("Invalid range. Only 0-90 allowed");

                        RuleFor(x => x.Data.DaysIncidentHistory)
                            .InclusiveBetween((ushort)0, (ushort)90)
                            .WithMessage("Invalid range. Only 0-90 allowed");
                    });
            }
        }
    }
}
