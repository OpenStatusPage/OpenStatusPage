using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace OpenStatusPage.Server.Persistence.Drivers
{
    public class PostgreSqlDbContext : ApplicationDbContext
    {
        private readonly IConfiguration _configuration;

        public PostgreSqlDbContext(DbContextOptions options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(_configuration["Storage:ConnectionString"]);
        }
    }
}
