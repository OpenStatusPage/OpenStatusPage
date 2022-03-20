using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Persistence;

namespace OpenStatusPage.Server.Application.Setup
{
    public class DatabaseInitializer : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly EnvironmentSettings _environmentSettings;

        public DatabaseInitializer(IServiceScopeFactory serviceScopeFactory, EnvironmentSettings environmentSettings)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _environmentSettings = environmentSettings;
        }

        /// <summary>
        /// Ensure the connected database has the latest migrations applied.
        /// If the database did not exist yet, it will be created.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            try
            {
                var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (_environmentSettings.IsTest)
                {
                    applicationDbContext.Database.EnsureDeleted();
                }

                if (applicationDbContext.Database.IsRelational())
                {
                    applicationDbContext.Database.Migrate();
                }
                else if (_environmentSettings.IsTest)
                {
                    applicationDbContext.Database.EnsureCreated();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Unable to initalize database. Please check that the datbase is available and your connection string is correct!", ex);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
