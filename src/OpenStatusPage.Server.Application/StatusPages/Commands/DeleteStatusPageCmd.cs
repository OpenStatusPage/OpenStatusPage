using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc.Attributes;

namespace OpenStatusPage.Server.Application.StatusPages.Commands
{
    [RequiresDbTransaction]
    public class DeleteStatusPageCmd : MessageBase
    {
        public string StatusPageId { get; set; }

        public class Handler : IRequestHandler<DeleteStatusPageCmd>
        {
            private readonly StatusPageService _statusPageService;

            public Handler(StatusPageService statusPageService)
            {
                _statusPageService = statusPageService;
            }

            public async Task<Unit> Handle(DeleteStatusPageCmd request, CancellationToken cancellationToken)
            {
                var statusPage = await _statusPageService.Get(request.StatusPageId).FirstOrDefaultAsync(cancellationToken);

                //Already deleted
                if (statusPage == null) return Unit.Value;

                await _statusPageService.DeleteAsync(statusPage);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteStatusPageCmd>
        {
            public Validator()
            {
                RuleFor(x => x.StatusPageId)
                    .NotEmpty()
                    .WithMessage("Missing StatusPageId");
            }
        }
    }
}
