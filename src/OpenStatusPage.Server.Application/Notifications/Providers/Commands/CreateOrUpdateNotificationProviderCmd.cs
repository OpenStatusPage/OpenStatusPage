using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

namespace OpenStatusPage.Server.Application.Notifications.Providers.Commands
{
    public class CreateOrUpdateNotificationProviderCmd : MessageBase
    {
        public NotificationProvider Data { get; set; }

        public class Handler : IRequestHandler<CreateOrUpdateNotificationProviderCmd>
        {
            private readonly NotificationProviderService _notificationProviderService;
            private readonly IMapper _mapper;

            public Handler(NotificationProviderService notificationProviderService, IMapper mapper)
            {
                _notificationProviderService = notificationProviderService;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(CreateOrUpdateNotificationProviderCmd request, CancellationToken cancellationToken)
            {
                var provider = await _notificationProviderService.Get(request.Data.Id).FirstOrDefaultAsync(cancellationToken);

                //Only apply the command if the entity is not already updated (can happen if multiple members share the same db)
                if (provider != null && provider.Version >= request.Data.Version) return Unit.Value;

                if (provider == null) //Handle creation
                {
                    provider = await (await _notificationProviderService.CreateAsync(request.Data)).FirstOrDefaultAsync(cancellationToken);

                    if (provider == null) throw new Exception("Could not create the provider.");
                }
                else //Handle update
                {
                    //Apply new values from data container
                    _mapper.Map(request.Data, provider);

                    //Save changes
                    await _notificationProviderService.UpdateAsync(provider);
                }

                return Unit.Value;
            }
        }
        public class Validator : AbstractValidator<CreateOrUpdateNotificationProviderCmd>
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
                            v.Add(new SmtpEmailProviderValidator());
                            v.Add(new WebhookProviderValidator());
                        });
                    });
            }
        }

        public class NotificationProviderValidator<T> : AbstractValidator<T> where T : NotificationProvider
        {
            public NotificationProviderValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Field Id is required.");

                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Field Name is required.");
            }
        }

        public class SmtpEmailProviderValidator : NotificationProviderValidator<SmtpEmailProvider>
        {
            public SmtpEmailProviderValidator()
            {
                RuleFor(x => x.Hostname)
                    .NotEmpty()
                    .WithMessage("Field Hostname is required.");

                RuleFor(x => x.Username)
                    .NotEmpty()
                    .WithMessage("Field Username is required.");

                RuleFor(x => x.Password)
                    .NotEmpty()
                    .WithMessage("Field Password is required.");
            }
        }

        public class WebhookProviderValidator : NotificationProviderValidator<WebhookProvider>
        {
            public WebhookProviderValidator()
            {
                RuleFor(x => x.Url)
                    .NotEmpty()
                    .WithMessage("Field Url is required.")
                    .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                    .WithMessage("Field Url value is not valid.");
            }
        }
    }
}
