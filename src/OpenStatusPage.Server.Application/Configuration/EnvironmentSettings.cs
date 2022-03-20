using Microsoft.Extensions.Configuration;

namespace OpenStatusPage.Server.Application.Configuration
{
    public class EnvironmentSettings
    {
        //Webserver settings
        public const string BIND = "BIND";
        public const string SSL_PATH = "CERTIFICATEPFX";
        public const string SSL_PASSWORD = "CERTIFICATEPASSWORD";
        public const string TRUST_PROXIES = "TRUSTPROXIES";
        public const string PORTVARIABLE = "PORTVARIABLE";

        //Cluster settings
        public const string ENDPOINT = "ENDPOINT";
        public const string API_KEY = "APIKEY";
        public const string CONNECT = "CONNECT";
        public const string TIMEOUT = "TIMEOUT";

        //Worker settings
        public const string ID = "ID";
        public const string TAGS = "TAGS";

        //Testing
        public const string TESTMODE = "TESTMODE";

        //Raw bind data
        public string? SslPath { get; set; }
        public string? SslPassword { get; set; }

        //Processed bind data
        public bool UseSSL { get; set; }
        public bool TrustProxies { get; set; }
        public List<Uri> BindUris { get; set; } = new();

        //Cluster data
        public Uri PublicEndpoint { get; set; }
        public string ApiKey { get; set; }
        public int ConnectionTimeout { get; set; }
        public List<Uri> ConnectEndpoints { get; set; }

        //Worker data
        public string Id { get; set; }
        public List<string> Tags { get; set; }

        public bool IsTest { get; set; }

        public static EnvironmentSettings Create(IConfiguration configuration)
        {
            var settings = new EnvironmentSettings();

            foreach (var uri in configuration.GetValue(BIND, "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    settings.BindUris.Add(new Uri(uri));
                }
                catch
                {
                }
            }

            settings.SslPath = configuration.GetValue<string?>(SSL_PATH, null);
            settings.SslPassword = configuration.GetValue<string?>(SSL_PASSWORD, null);

            //Add default binds
            if (settings.BindUris.Count == 0)
            {
                var port = configuration.GetValue<int?>(configuration.GetValue<string?>(PORTVARIABLE, null) ?? "PORT", null);
                var isContainer = configuration.GetValue("DOTNET_RUNNING_IN_CONTAINER", false);

                if (!port.HasValue || settings.SslPath == null)
                {
                    //Only bind to normal http if the single bind port is not combined with an ssl certificate
                    settings.BindUris.Add(new Uri($"http://{(isContainer ? "0.0.0.0" : "127.0.0.1")}:{port ?? 80}"));
                }

                if (settings.SslPath != null)
                {
                    settings.BindUris.Add(new Uri($"https://{(isContainer ? "0.0.0.0" : "127.0.0.1")}:{port ?? 443}"));
                }
            }

            settings.UseSSL = settings.BindUris.Any(x => x.Scheme.ToLowerInvariant().Equals("https"));

            if (settings.UseSSL && string.IsNullOrWhiteSpace(settings.SslPath))
            {
                throw new Exception($"No SSL certiciate configured. Specify --{SSL_PATH}=\"YOUR-PATH-TO-PFX\" or only listen on HTTP");
            }

            settings.TrustProxies = configuration.GetValue(TRUST_PROXIES, false);

            //Process cluster data
            try
            {
                settings.PublicEndpoint = new Uri(configuration.GetValue(ENDPOINT, settings.BindUris.FirstOrDefault()?.ToString()!));
            }
            catch
            {
                throw new Exception($"Invalid public endpoint. Specify --{ENDPOINT}=\"http(s)://your-server.tld\" with a valid URI");
            }

            settings.ApiKey = configuration.GetValue(API_KEY, "");

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new Exception($"Missing cluster access/api key. " +
                    $"To fix this, add --{API_KEY}=\"XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX\". " +
                    $"You can generate them on websites like https://guidgenerator.com");
            }

            settings.ConnectionTimeout = configuration.GetValue(TIMEOUT, 2500);

            settings.ConnectEndpoints = new();
            foreach (var item in configuration.GetValue(CONNECT, "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    settings.ConnectEndpoints.Add(new Uri(item));
                }
                catch
                {
                }
            }

            //Process worker data
            settings.Id = configuration.GetValue(ID, Guid.NewGuid().ToString());

            var tags = configuration.GetValue(TAGS, "");

            if (!string.IsNullOrWhiteSpace(tags))
            {
                settings.Tags = tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
            else
            {
                settings.Tags = new();
            }

            settings.IsTest = configuration.GetValue(TESTMODE, false);

            return settings;
        }
    }
}
