using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Serialization;

using Wukong.Options;
using Wukong.Services;
using Wukong.Models;
using Wukong.Repositories;

namespace Wukong
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddDbContext<UserSongListContext>(options =>
            {
                options.UseSqlite(Configuration["SqliteConnectionString"]);
            });

            services.AddDataProtection();
            services.AddOptions();
            services.AddCors();

            // Dependency injection
            services.AddScoped<IUserSongListRepository, UserSongListRepository>();

            // UserOption:PublicKey
            services.Configure<UserOption>(options =>
            {
                options.PublicKey = Configuration["UserOption:PublicKey"];
            });

            services.Configure<ProviderOption>(options =>
            {
                options.Url = Configuration["ProviderOption:Url"];
            });
            // services.Configure<MvcOptions>(options =>
            // {
            //     options.Filters.Add(new RequireHttpsAttribute ());
            // });
            services.AddMvc()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });
            services.AddDistributedMemoryCache();
            services.AddSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            SocketManager.Manager.PublicKey = Configuration["UserOption:PublicKey"];
            SocketManager.Manager.LoggerFactory = loggerFactory;

            app.UseApplicationInsightsRequestTelemetry();
            app.UseApplicationInsightsExceptionTelemetry();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });
            app.UseCors(builder => builder.WithOrigins(new string[] { "http://127.0.0.1:8080", "http://localhost:8080" }).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            app.UseCookieAuthentication(Options.AuthenticationOptions.CookieAuthenticationOptions());
            app.UseGoogleAuthentication(Options.OAuthOptions.GoogleOAuthOptions(Configuration["Authentication:Google:ClientId"],
                Configuration["Authentication:Google:ClientSecret"]));
            app.UseWebSockets();
            app.Use(SocketManager.Manager.WebsocketHandler);
            app.UseSession();
            app.UseMvc();
        }
    }
}
