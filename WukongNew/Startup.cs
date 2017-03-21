using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Wukong.Models;
using Wukong.Options;
using Wukong.Repositories;
using Wukong.Services;

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

                builder.AddUserSecrets<SecretOptions>();
                builder.AddUserSecrets<ProviderOptions>();
                builder.AddUserSecrets<ApplicationInsightsOptions>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        private SettingOptions Settings = new SettingOptions();

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddDataProtection();
            services.AddOptions();
            services.AddCors();

            // Dependency injection
            services.AddScoped<IUserSongListRepository, UserSongListRepository>();

            services.AddSingleton<ISocketManager, SocketManager>();
            services.AddSingleton<IProvider, Provider>();
            services.AddSingleton<IChannelManager, ChannelManager>();
            services.AddSingleton<IStorage, Storage>();
            services.AddSingleton<IUserManager, UserManager>();
            services.AddScoped<IUserService, UserService>();
            services.Configure<SettingOptions>(Configuration);

            Configuration.Bind(Settings);

            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            services.AddMvc()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });
            services.AddDistributedMemoryCache();
            services.AddSession();
            services.AddDbContext<UserSongListContext>(options =>
            {
                options.UseSqlite(Settings.SqliteConnectionString);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseSession();
            app.UseApplicationInsightsRequestTelemetry();
            app.UseApplicationInsightsExceptionTelemetry();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });
            app.UseCors(builder => builder.WithOrigins("http://127.0.0.1:8080", "http://localhost:8080").AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            app.UseCookieAuthentication(Options.AuthenticationOptions.CookieAuthenticationOptions());
            
            app.UseMicrosoftAccountAuthentication(OAuthProviderOptions.MicrosoftOAuthOptions(Settings.Authentication.Microsoft));
            app.UseOAuthAuthentication(OAuthProviderOptions.GitHubOAuthOptions(Settings.Authentication.GitHub));
            app.UseGoogleAuthentication(OAuthProviderOptions.GoogleOAuthOptions(Settings.Authentication.Google));

            app.UseWebSockets();
            app.UseMiddleware<SocketManagerMiddleware>();
            app.UseMiddleware<UserManagerMiddleware>();

            app.UseMvc();
        }
    }
}
