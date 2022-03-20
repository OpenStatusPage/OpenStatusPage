using OpenStatusPage.Server.Domain.Entities.StatusHistory;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.StatusHistory
{
    public class StatusHistoryService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public StatusHistoryService(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public IQueryable<StatusHistoryRecord> Get() => _applicationDbContext.StatusHistoryRecords;

        public async Task DeleteAsync(StatusHistoryRecord historyRecord)
        {
            _applicationDbContext.Remove(historyRecord);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task CreateAsync(StatusHistoryRecord historyRecord)
        {
            _applicationDbContext.Add(historyRecord);

            await _applicationDbContext.SaveChangesAsync();
        }
    }
}
