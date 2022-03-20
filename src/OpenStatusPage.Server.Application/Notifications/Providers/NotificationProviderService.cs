using AutoMapper;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Notifications.Providers
{
    public class NotificationProviderService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public NotificationProviderService(ApplicationDbContext applicationDbContext, IMapper mapper)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
        }

        public IQueryable<NotificationProvider> Get() => _applicationDbContext.NotificationProviders;

        public IQueryable<NotificationProvider> Get(string id) => _applicationDbContext.NotificationProviders.Where(x => x.Id == id);

        public async Task UpdateAsync(NotificationProvider provider) => await _applicationDbContext.SaveChangesAsync();

        public async Task DeleteAsync(NotificationProvider provider)
        {
            _applicationDbContext.Remove(provider);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<IQueryable<NotificationProvider>> CreateAsync(NotificationProvider providerData)
        {
            var provider = await _applicationDbContext.CreateEntityAsync(x => x.NotificationProviders, false, providerData.GetType());

            _mapper.Map(providerData, provider);

            _applicationDbContext.Add(provider);

            await _applicationDbContext.SaveChangesAsync();

            return Get(provider.Id);
        }
    }
}
