using AutoMapper;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Monitors
{
    public class MonitorService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public MonitorService(ApplicationDbContext applicationDbContext, IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
        }

        public IQueryable<MonitorBase> Get() => _applicationDbContext.Monitors;

        public IQueryable<MonitorBase> Get(string id) => _applicationDbContext.Monitors.Where(x => x.Id == id);

        public ApplicationDbContext GetDbContext() => _applicationDbContext;

        public async Task UpdateAsync(MonitorBase monitor) => await _applicationDbContext.SaveChangesAsync();

        public async Task DeleteAsync(MonitorBase monitor)
        {
            _applicationDbContext.Remove(monitor);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<IQueryable<MonitorBase>> CreateAsync(MonitorBase monitorData)
        {
            var monitor = await _applicationDbContext.CreateEntityAsync(x => x.Monitors, false, monitorData.GetType());

            _mapper.Map(monitorData, monitor);

            _applicationDbContext.Add(monitor);

            await _applicationDbContext.SaveChangesAsync();

            return Get(monitor.Id);
        }
    }
}
