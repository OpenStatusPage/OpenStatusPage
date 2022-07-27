using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application;
using OpenStatusPage.Server.Application.Authentication;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Communication.Http;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Setup;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Server.Persistence.Drivers;
using OpenStatusPage.Shared.Interfaces;
using System.Net;

namespace OpenStatusPage.Server
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //Add global appsettings
            var environmentSettings = EnvironmentSettings.Create(Configuration);
            services.AddSingleton(environmentSettings);

            ////Used for migrations
            //var dbDriver = "sqlite";
            //var connectionString = "Data Source=local.db";
            ////var dbDriver = "postgresql";
            ////var connectionString = "User ID=postgres;Host=localhost;Port=5432;Database=OpenStatusPage;";
            //Configuration["Storage:Driver"] = dbDriver;
            //Configuration["Storage:ConnectionString"] = connectionString;

            //Setup database drivers
            var driver = Configuration["Storage:Driver"] ?? string.Empty;

            if (driver.StartsWith("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<ApplicationDbContext, SQLiteDbContext>(options => SetDbContextOptions(options));
            }
            else if (driver.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<ApplicationDbContext, PostgreSqlDbContext>(options => SetDbContextOptions(options));
            }
            else
            {
                services.AddDbContext<ApplicationDbContext, InMemoryDbContext>(options => SetDbContextOptions(options));
            }

            static void SetDbContextOptions(DbContextOptionsBuilder options)
            {
                //options.UseLazyLoadingProxies();

                //options.EnableDetailedErrors();
                //options.EnableSensitiveDataLogging();
            }

            //Ensure database migration
            services.AddHostedService<DatabaseInitializer>();

            var applicationAssembly = ApplicationAssembly.Reference;

            //Add services to the container.
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddMediatR(applicationAssembly);
            services.AddAutoMapper(applicationAssembly);
            services.AddAutoMapper(typeof(Startup).Assembly);
            services.AddValidatorsFromAssembly(applicationAssembly);
            services
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(FluentValidationPipelineBehavior<,>));
            //.AddScoped(typeof(IPipelineBehavior<,>), typeof(DbTransactionBehavior<,>));

            //Add transient services
            foreach (var transient in applicationAssembly.GetTypes().Where(x => x.IsAssignableTo(typeof(ITransientService)) && !x.IsInterface))
            {
                services.AddTransient(transient);
            }

            //Add scoped services
            foreach (var scoped in applicationAssembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IScopedService)) && !x.IsInterface))
            {
                services.AddScoped(scoped);
            }

            //Add singleton services
            foreach (var singleton in applicationAssembly.GetTypes().Where(x => x.IsAssignableTo(typeof(ISingletonService)) && !x.IsInterface))
            {
                services.AddSingleton(singleton);

                //Register singleton as hosted service if it is one
                if (singleton.IsAssignableTo(typeof(IHostedService)))
                {
                    services.AddSingleton<IHostedService>(provider => provider.GetService(singleton) as IHostedService);
                }
            }

            //Entry for cluster service configuration
            services.AddSingleton<INetworkConnector, HttpConnector>();
            services.AddClusterServices(Configuration);

            //Forward https connections in case this app runs behind a proxy server
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;

                //Trust local host as default proxy server
                options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));

                if (environmentSettings.TrustProxies)
                {
                    //Trust any network interface to send forward headers
                    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("0.0.0.0"), 0));
                }
            });

            //Disable CORS. Trust is based on access keys. Calls could come from yet unknown standalone wasm PWA apps
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            //Authentication
            services
                .AddAuthentication(ApiKeyAuthenticationOptions.SCHEME)
                .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.SCHEME, options => { });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var environmentSettings = app.ApplicationServices.GetRequiredService<EnvironmentSettings>();

            app.UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else if (environmentSettings.UseSSL)
            {
                app.UseHsts();
            }

            if (environmentSettings.UseSSL) app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();

            //Enforce authorization for all public facing apis
            app.UseWhen(x => x.Request.Path.StartsWithSegments("/api"), conditionalApp =>
            {
                conditionalApp.UseAuthorization();
            });

            app.UseClusterService();

            app.UseEndpoints(endpoints =>
            {
                endpoints.UseHttpConnector();
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
