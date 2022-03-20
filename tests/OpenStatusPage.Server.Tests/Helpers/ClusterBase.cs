using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Helpers;

[ExcludeFromCodeCoverage(Justification = "Test fixture itself does not need to be tested")]
public class ClusterBase
{
    protected static readonly object _lock = new();
    protected static readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(1);

    protected static IHost CreateClusterMember(int port, IDictionary<string, string> configuration, ITestOutputHelper testOutput)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseKestrel(options => options.ListenLocalhost(port))
                .UseStartup<Startup>())
            .ConfigureAppConfiguration((hostBuilder, configBuilder) => configBuilder.AddInMemoryCollection(configuration))
            .ConfigureLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddFilter((provider, category, logLevel) => category.Contains("OpenStatusPage") && !category.Contains("ApiKeyAuthenticationHandler") || logLevel >= LogLevel.Error) //External logging only in case of error
                .AddDebug()
            )
            .ConfigureServices(services => services.AddLogging((builder) => builder.AddXUnit(testOutput)))
            .UseClusterService()
            .Build();
    }

    public static IHost CreateLeader(ITestOutputHelper testOutput)
    {
        var port = FindNextPort();

        var config = new Dictionary<string, string>
        {
            { "Testmode", "true" },
            { "Port", port.ToString() },
            { "Timeout", _connectionTimeout.TotalMilliseconds.ToString() },
            { "ApiKey", Guid.NewGuid().ToString() },
            { "Id", Guid.NewGuid().ToString() },
            { "Tags", $"local,testing,leader" },
        };

        return CreateClusterMember(port, config, testOutput);
    }

    public static IHost CreateFollower(IHost leader, ITestOutputHelper testOutput)
    {
        var port = FindNextPort();
        var leaderSettings = leader.Services.GetRequiredService<EnvironmentSettings>();

        var config = new Dictionary<string, string>
        {
            { "Testmode", "true" },
            { "Port", port.ToString() },
            { "Connect", leaderSettings.PublicEndpoint.ToString() },
            { "Timeout", leaderSettings.ConnectionTimeout.ToString() },
            { "ApiKey", leaderSettings.ApiKey },
            { "Id", Guid.NewGuid().ToString() },
            { "Tags", $"local,testing,follower" },
        };

        return CreateClusterMember(port, config, testOutput);
    }

    protected static int FindNextPort()
    {
        int port;

        lock (_lock)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            port = ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        return port;
    }
}
