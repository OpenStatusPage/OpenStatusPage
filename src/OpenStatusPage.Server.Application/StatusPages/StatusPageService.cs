using AutoMapper;
using OpenStatusPage.Server.Domain.Entities.StatusPages;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.StatusPages
{
    public class StatusPageService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public StatusPageService(ApplicationDbContext applicationDbContext, IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
        }

        public IQueryable<StatusPage> Get() => _applicationDbContext.StatusPages;

        public IQueryable<StatusPage> Get(string id) => _applicationDbContext.StatusPages.Where(x => x.Id == id);

        public async Task UpdateAsync(StatusPage statusPage) => await _applicationDbContext.SaveChangesAsync();

        public async Task DeleteAsync(StatusPage statusPage)
        {
            _applicationDbContext.Remove(statusPage);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<IQueryable<StatusPage>> CreateAsync(StatusPage statusPageData)
        {
            var statusPage = await _applicationDbContext.CreateEntityAsync(x => x.StatusPages, false);

            _mapper.Map(statusPageData, statusPage);

            _applicationDbContext.Add(statusPage);

            await _applicationDbContext.SaveChangesAsync();

            return Get(statusPage.Id);
        }
    }
}
