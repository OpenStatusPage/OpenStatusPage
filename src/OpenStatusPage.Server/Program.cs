using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace OpenStatusPage.Server;

[ExcludeFromCodeCoverage(Justification = "Tests use their own startup process to to start non blocking, with different loggen and on local host only")]
public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Terminated due to exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        EnvironmentSettings environmentSettings = null!;

        return Host.CreateDefaultBuilder(args)
            .ConfigureHostOptions((hostBuilderContext, hostOptions) => environmentSettings = EnvironmentSettings.Create(hostBuilderContext.Configuration))
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
            )
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel(options =>
                {
                    foreach (var bindUri in environmentSettings.BindUris)
                    {
                        options.Listen(IPAddress.Parse(bindUri.Host), bindUri.Port, options =>
                        {
                            if (!bindUri.Scheme.ToLowerInvariant().Equals("https")) return;

                            if (!string.IsNullOrWhiteSpace(environmentSettings.SslPath))
                            {
                                if (!string.IsNullOrWhiteSpace(environmentSettings.SslPassword))
                                {
                                    options.UseHttps(environmentSettings.SslPath, environmentSettings.SslPassword);
                                }
                                else
                                {
                                    options.UseHttps(environmentSettings.SslPath);
                                }
                            }
                            else
                            {
                                options.UseHttps();
                            }
                        });
                    }
                });

                webBuilder.UseStartup<Startup>();
            })
            .UseClusterService();
    }
}
