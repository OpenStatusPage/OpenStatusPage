using AutoMapper;
using OpenStatusPage.Server.Domain.Entities.Configuration;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Configuration
{
    public class ApplicationSettingsService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public ApplicationSettingsService(ApplicationDbContext applicationDbContext, IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
        }

        public IQueryable<ApplicationSettings> Get() => _applicationDbContext.ApplicationSettings;

        public IQueryable<ApplicationSettings> Get(string id) => _applicationDbContext.ApplicationSettings.Where(x => x.Id == id);

        public async Task UpdateAsync(ApplicationSettings applicationSettings) => await _applicationDbContext.SaveChangesAsync();

        public async Task<IQueryable<ApplicationSettings>> CreateAsync(ApplicationSettings applicationSettingsData)
        {
            var applicationSettings = await _applicationDbContext.CreateEntityAsync(x => x.ApplicationSettings, false);

            _mapper.Map(applicationSettingsData, applicationSettings);

            _applicationDbContext.Add(applicationSettings);

            await _applicationDbContext.SaveChangesAsync();

            return Get(applicationSettings.Id);
        }
    }
}
