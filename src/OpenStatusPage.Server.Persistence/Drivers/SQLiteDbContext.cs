using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace OpenStatusPage.Server.Persistence.Drivers;

public class SQLiteDbContext : ApplicationDbContext
{
    private readonly IConfiguration _configuration;

    public SQLiteDbContext(DbContextOptions options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(_configuration["Storage:ConnectionString"]);
    }
}
