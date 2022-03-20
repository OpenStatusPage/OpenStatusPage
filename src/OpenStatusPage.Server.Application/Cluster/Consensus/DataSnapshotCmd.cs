using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Persistence;

namespace OpenStatusPage.Server.Application.Cluster.Consensus
{
    public class DataSnapshotCmd : MessageBase
    {
        public Dictionary<string, List<MessageBase>> DataConstructionMessages { get; set; }

        public static async Task<DataSnapshotCmd> BuildAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var snapshot = new DataSnapshotCmd()
            {
                DataConstructionMessages = new()
            };

            var dataProviders = ApplicationAssembly.Reference
                .GetTypes()
                .Where(x => x.IsAssignableTo(typeof(ISnapshotDataProvider)) && !x.IsInterface)
                .ToList();

            using var scope = serviceProvider.CreateScope();

            foreach (var dataProvider in dataProviders)
            {
                var providerInstance = scope.ServiceProvider.GetRequiredService(dataProvider) as ISnapshotDataProvider;

                snapshot.DataConstructionMessages[dataProvider.Name] = await providerInstance.GetDataAsync(cancellationToken);
            }

            return snapshot;
        }

        public class Handler : IRequestHandler<DataSnapshotCmd>
        {
            private readonly IServiceProvider _serviceProvider;

            public Handler(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public async Task<Unit> Handle(DataSnapshotCmd request, CancellationToken cancellationToken)
            {
                var dataProviders = ApplicationAssembly.Reference
                    .GetTypes()
                    .Where(x => x.IsAssignableTo(typeof(ISnapshotDataProvider)) && !x.IsInterface)
                    .OrderBy(x =>
                    {
                        var attribute = Attribute.GetCustomAttribute(x.GetMethod("ApplyDataAsync")!, typeof(SnapshotApplyDataOrderAttribute));

                        if (attribute is SnapshotApplyDataOrderAttribute snapshotDataOrder) return snapshotDataOrder.OrderIndex;

                        return 0;
                    })
                    .ToList();

                using var scope = _serviceProvider.CreateScope();

                using var transaction = await scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>()
                    .Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    foreach (var dataProvider in dataProviders)
                    {
                        var providerInstance = scope.ServiceProvider.GetRequiredService(dataProvider) as ISnapshotDataProvider;

                        if (request.DataConstructionMessages.ContainsKey(dataProvider.Name))
                        {
                            await providerInstance.ApplyDataAsync(request.DataConstructionMessages[dataProvider.Name], cancellationToken);
                        }
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }

                return Unit.Value;
            }
        }
    }
}
