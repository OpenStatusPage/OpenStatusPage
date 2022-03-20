using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Http;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types
{
    public class HttpMonitorCheck : MonitorCheckBase
    {
        protected static async Task<HttpRequestMessage> BuildMessageAsync(HttpMethod method, string url, List<IGrouping<string, string[]>> headers, string? body)
        {
            var message = new HttpRequestMessage(method, url);

            foreach (var header in headers)
            {
                message.Headers.Add(header.Key, header.SelectMany(x => x));
            }

            if (!string.IsNullOrWhiteSpace(body)) message.Content = new StringContent(body);

            return message;
        }

        protected override async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken)
        {
            if (monitor is not HttpMonitor httpMonitor) throw new Exception($"Invalid monitor type assigned to {nameof(HttpMonitorCheck)}");

            //Method
            var method = new HttpMethod(Enum.GetName(httpMonitor.Method)!);

            //Headers
            var headers = httpMonitor.Headers?
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(pair => pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) //Split only on first =
                .GroupBy(x => x[0]) //Group by header key
                .ToList() ?? new();

            //Authenication
            ICredentials? credentials = httpMonitor.AuthenticationScheme switch
            {
                HttpAuthenticationScheme.None or HttpAuthenticationScheme.Bearer => null,
                HttpAuthenticationScheme.Basic or HttpAuthenticationScheme.Digest => new NetworkCredential(httpMonitor.AuthenticationBase, httpMonitor.AuthenticationAdditional),
                _ => throw new NotImplementedException(),
            };

            //Setup ceritficate checks
            X509Certificate2 certificate = null!;
            X509Chain certificateChain = null!;
            SslPolicyErrors sslPolicyErrors = default;

            var redirects = httpMonitor.MaxRedirects;

            using var httpClient = new HttpClient(new HttpClientHandler
            {
                //Send authentication header
                PreAuthenticate = credentials != null,

                Credentials = credentials,

                //Fetch SSL info and pass out of the callback
                CheckCertificateRevocationList = true,
                ServerCertificateCustomValidationCallback = (request, cert, chain, policyErrors) =>
                {
                    certificate = cert!;
                    certificateChain = chain!;
                    sslPolicyErrors = policyErrors;
                    return true;
                },

                //Setup manual redirect handling
                AllowAutoRedirect = false,
            });

            //Set bearer manually, because NetworkCredential can handle hamdle it
            if (httpMonitor.AuthenticationScheme == HttpAuthenticationScheme.Bearer)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", httpMonitor.AuthenticationBase);
            }

            //Setup timeout
            if (httpMonitor.Timeout.HasValue) httpClient.Timeout = httpMonitor.Timeout.Value;

            try
            {
                //Start timer to measure response time
                var stopwatch = new Stopwatch();

                //Send request and follow redirects manually (because net core does not allow https->http downgrade redirects
                HttpResponseMessage? response = null!;
                var requestUri = httpMonitor.Url;
                while (true)
                {
                    //Only measure the last redirect response time, so reset it every time we keep following one
                    stopwatch.Restart();
                    response = await httpClient.SendAsync(await BuildMessageAsync(method, requestUri, headers, httpMonitor.Body), cancellationToken);
                    stopwatch.Stop();

                    if (redirects > 0 && HttpStatusCode.Ambiguous <= response.StatusCode && response.StatusCode <= HttpStatusCode.PermanentRedirect)
                    {
                        requestUri = response.Headers.Location.AbsoluteUri;

                        redirects--;

                        continue;
                    }

                    break;
                }

                //Get the response time
                var responseTime = stopwatch.Elapsed.TotalMilliseconds;

                //Only read body if there is a rule that needs it 
                var body = httpMonitor.Rules.Any(x => x is ResponseBodyRule) ? await response.Content.ReadAsStringAsync(cancellationToken) : "";

                //Evaluate all the rules
                foreach (var rule in httpMonitor.Rules.OrderBy(x => x.OrderIndex))
                {
                    switch (rule)
                    {
                        case ResponseTimeRule responseTimeRule:
                        {
                            if (NumberCompareHelper.Compare(responseTime, responseTimeRule.ComparisonType, responseTimeRule.ComparisonValue))
                            {
                                return responseTimeRule.ViolationStatus;
                            }

                            break;
                        }

                        case ResponseBodyRule responseBodyRule:
                        {
                            if (StringCompareHelper.Compare(body, responseBodyRule.ComparisonType, responseBodyRule.ComparisonValue))
                            {
                                return responseBodyRule.ViolationStatus;
                            }

                            break;
                        }

                        case ResponseHeaderRule responseHeaderRule:
                        {
                            var headerValues = response.Headers.Where(x => x.Key.Equals(responseHeaderRule.Key, StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Value).ToList();

                            foreach (var value in headerValues)
                            {
                                if (StringCompareHelper.Compare(value, responseHeaderRule.ComparisonType, responseHeaderRule.ComparisonValue!))
                                {
                                    return responseHeaderRule.ViolationStatus;
                                }
                            }

                            break;
                        }

                        case SslCertificateRule sslCertificateRule:
                        {
                            //No matter the rule, if the certificate is no present it will be violated
                            if (certificate == null)
                            {
                                return sslCertificateRule.ViolationStatus;
                            }

                            if (sslCertificateRule.CheckType == SslCertificateCheckType.NotValid)
                            {
                                //Policy errors means that it is not considered valid no need to chekc anything further
                                if (sslPolicyErrors != SslPolicyErrors.None) return sslCertificateRule.ViolationStatus;

                                //If the certificate is not valid for the required timespan - report it
                                if (sslCertificateRule.MinValidTimespan.HasValue &&
                                    DateTime.UtcNow.Add(sslCertificateRule.MinValidTimespan.Value) > certificate.NotAfter)
                                {
                                    return sslCertificateRule.ViolationStatus;
                                }
                            }

                            break;
                        }

                        case StatusCodeRule statusCodeRule:
                        {
                            if (statusCodeRule.UpperRangeValue.HasValue &&
                                !(statusCodeRule.Value <= (int)response.StatusCode && (int)response.StatusCode <= statusCodeRule.UpperRangeValue.Value))
                            {
                                return statusCodeRule.ViolationStatus;
                            }
                            else if (statusCodeRule.Value != (int)response.StatusCode)
                            {
                                return statusCodeRule.ViolationStatus;
                            }

                            break;
                        }

                        default: throw new NotImplementedException();
                    }
                }
            }
            catch
            {
                return ServiceStatus.Unavailable;
            }

            //Default result
            return ServiceStatus.Available;
        }
    }
}
