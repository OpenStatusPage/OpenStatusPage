using Blazored.LocalStorage;
using System.Net.Http.Json;

namespace OpenStatusPage.Client.Application
{
    public class ClusterEndpointsService : IDisposable
    {
        protected static readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        protected readonly ISyncLocalStorageService _localStorage;

        protected readonly Timer _timer;

        public HashSet<Uri> Endpoints { get; protected set; }

        public ClusterEndpointsService(ISyncLocalStorageService localStorage, HostEnvironmentParameters parameters)
        {
            _localStorage = localStorage;

            Endpoints = _localStorage.GetItem<HashSet<Uri>>("KnownEndpoints") ?? new();

            //Add the host address if not already part of the list
            Endpoints.Add(new Uri(parameters.BaseAddress));

            _timer = new(async (state) => await RefreshEndpointsAsync(), null, TimeSpan.Zero, _interval);
        }

        protected async Task RefreshEndpointsAsync()
        {
            var httpClient = new HttpClient();

            var randomOrder = Endpoints.OrderBy(item => Random.Shared.Next()).ToList();

            foreach (var endpoint in randomOrder)
            {
                try
                {
                    var endpoints = await httpClient.GetFromJsonAsync<HashSet<Uri>>($"{endpoint}api/v1/ClusterMembers/public/endpoints");

                    if (endpoints != null)
                    {
                        Endpoints = endpoints;

                        _localStorage.SetItem("KnownEndpoints", Endpoints);
                        break;
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
