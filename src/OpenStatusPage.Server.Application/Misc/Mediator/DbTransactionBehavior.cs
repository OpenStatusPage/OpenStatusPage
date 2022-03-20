using MediatR;
using OpenStatusPage.Server.Application.Misc.Attributes;
using OpenStatusPage.Server.Persistence;

namespace OpenStatusPage.Server.Application.Misc.Mediator
{
    public class DbTransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly ApplicationDbContext _storage;

        public DbTransactionBehavior(ApplicationDbContext storage)
        {
            _storage = storage;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (_storage.Database.CurrentTransaction != null || !request.GetType().IsDefined(typeof(RequiresDbTransactionAttribute), false))
            {
                //Ingore transcation if already in one or none was requested
                return await next();
            }
            else
            {
                using var transaction = await _storage.Database.BeginTransactionAsync();

                try
                {
                    var result = await next();
                    await transaction.CommitAsync();

                    return result;

                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
