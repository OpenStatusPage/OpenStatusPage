using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Notifications.Providers.Commands
{
    public class DeleteNotificationProviderCmd : MessageBase
    {
        public string ProviderId { get; set; }

        public class Handler : IRequestHandler<DeleteNotificationProviderCmd>
        {
            private readonly NotificationProviderService _notificationProviderService;

            public Handler(NotificationProviderService notificationProviderService)
            {
                _notificationProviderService = notificationProviderService;
            }

            public async Task<Unit> Handle(DeleteNotificationProviderCmd request, CancellationToken cancellationToken)
            {
                var provider = await _notificationProviderService.Get(request.ProviderId).FirstOrDefaultAsync(cancellationToken);

                //Already deleted
                if (provider == null) return Unit.Value;

                await _notificationProviderService.DeleteAsync(provider);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteNotificationProviderCmd>
        {
            public Validator()
            {
                RuleFor(x => x.ProviderId)
                    .NotEmpty()
                    .WithMessage("Missing ProviderId");
            }
        }
    }
}
