using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OpenStatusPage.Client.Application
{
    public class TransparentHttpClient
    {
        private readonly ClusterEndpointsService _clusterEndpointsService;

        private readonly HttpClient _http;

        protected HashSet<Uri> KnownEndpoints { get; set; } = new();

        public DateTimeOffset? LastEndpointSync { get; set; }

        protected Queue<Uri> RoundRobinQueue { get; set; }

        public TransparentHttpClient(ClusterEndpointsService clusterEndpointsService)
        {
            _clusterEndpointsService = clusterEndpointsService;
            _http = new();
        }

        protected void RefreshEndpoints()
        {
            if (LastEndpointSync.HasValue && (DateTimeOffset.Now - LastEndpointSync.Value) < TimeSpan.FromSeconds(10)) return;

            LastEndpointSync = DateTimeOffset.Now;

            var newEndpoints = _clusterEndpointsService.Endpoints.ToHashSet(); //Take a copy from the endpoint service to work on

            //New endpoints match the old ones (order does not matter), so no change.
            if (KnownEndpoints.SetEquals(newEndpoints)) return;

            KnownEndpoints = newEndpoints;
            RoundRobinQueue = new Queue<Uri>(newEndpoints);
        }

        protected bool TryGetNextEndpoint(Uri first, out Uri endpoint)
        {
            endpoint = null!;

            //If the next would be the first one we knew, we stop. We went through all known endpoints
            if (RoundRobinQueue.Peek().Equals(first)) return false;

            //Take the next endpoint
            endpoint = RoundRobinQueue.Dequeue();

            //Put it back at the end of the queue
            RoundRobinQueue.Enqueue(endpoint);

            return true;
        }

        public async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string requestUri, HeaderEntry header = default!, bool redirectToLeader = true, bool throwExceptions = false, CancellationToken cancellationToken = default)
        {
            return await SendAsync<TResponse>(method, requestUri, null!, header, redirectToLeader, throwExceptions, cancellationToken);
        }

        public async Task<TResponse> SendAsync<TRequest, TResponse>(HttpMethod method, string requestUri, TRequest? body, HeaderEntry header = default!, bool redirectToLeader = true, bool throwExceptions = false, CancellationToken cancellationToken = default)
        {
            return await SendAsync<TResponse>(method, requestUri, body!, header, redirectToLeader, throwExceptions, cancellationToken);
        }

        public async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string requestUri, object body, HeaderEntry header = default!, bool redirectToLeader = true, bool throwExceptions = false, CancellationToken cancellationToken = default)
        {
            RefreshEndpoints();

            Uri first = null!;

            while (TryGetNextEndpoint(first, out var endpoint))
            {
                if (cancellationToken.IsCancellationRequested) break;

                //Remember the first endpoint we tried in this loop, to stop once we went through all known ones
                first ??= endpoint;

                try
                {
                    var request = new HttpRequestMessage(method, $"{endpoint}{requestUri}");

                    //Write header if we have one
                    if (header != default) request.Headers.Add(header.Key, header.Value);

                    if (redirectToLeader) request.Headers.Add("X-Redirect-Leader", "true");

                    if (cancellationToken.IsCancellationRequested) break;

                    //Write body if we have one
                    if (body != null)
                    {
                        request.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType()), Encoding.UTF8, "application/json");
                    }

                    if (cancellationToken.IsCancellationRequested) break;

                    var result = await _http.SendAsync(request, cancellationToken);

                    if (!result.IsSuccessStatusCode && throwExceptions)
                    {
                        throw new HttpRequestException(null, null, result.StatusCode);
                    }

                    return await result.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    //Forward unauthorized exception to caller scope
                    if (throwExceptions) throw;
                }
                catch (Exception ex)
                {
                }
            }

            return default;
        }

        public class HeaderEntry
        {
            public string Key { get; set; }

            public string Value { get; set; }

            public HeaderEntry()
            {
            }

            public HeaderEntry(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
