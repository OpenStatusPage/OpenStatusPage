using AutoMapper;
using OpenStatusPage.Server.Domain.Entities.Incidents;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Incidents
{
    public class IncidentService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public IncidentService(ApplicationDbContext applicationDbContext, IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
        }

        public IQueryable<Incident> Get() => _applicationDbContext.Incidents;

        public IQueryable<Incident> Get(string id) => _applicationDbContext.Incidents.Where(x => x.Id == id);

        public async Task UpdateAsync(Incident incident) => await _applicationDbContext.SaveChangesAsync();

        public async Task DeleteAsync(Incident incident)
        {
            _applicationDbContext.Remove(incident);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<IQueryable<Incident>> CreateAsync(Incident incidentData)
        {
            var incident = await _applicationDbContext.CreateEntityAsync(x => x.Incidents, false);

            _mapper.Map(incidentData, incident);

            _applicationDbContext.Add(incident);

            await _applicationDbContext.SaveChangesAsync();

            return Get(incident.Id);
        }
    }
}
