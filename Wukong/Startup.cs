using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using Wukong.Models;
using Wukong.Options;
using Wukong.Repositories;
using Wukong.Services;
using Wukong.Utilities;

namespace Wukong
{
    public class Startup
    {
        private IConfigurationRoot Configuration { get; }
        private readonly SettingOptions Settings = new SettingOptions();

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
            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
            Configuration = builder.Build();
        }



        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SettingOptions>(Configuration);
            Configuration.Bind(Settings);

            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            // Use redis to store data protection key and session if necessary.
            var redisConnection = RedisConnection.GetConnectionString(Settings.RedisConnectionString);
            if (!String.IsNullOrEmpty(redisConnection))
            {
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                services.AddDataProtection().PersistKeysToRedis(redis, "DataProtection-Keys");
                services.AddDistributedRedisCache(option =>
                {
                    option.Configuration = redisConnection;
                    option.InstanceName = "master";
                });
            } else
            {
                services.AddDataProtection();
                services.AddDistributedMemoryCache();
            }
            services.AddSession();

            services.AddOptions();
            services.AddCors();

            // Dependency injection.
            services.AddScoped<IUserSongListRepository, UserSongListRepository>();
            services.AddScoped<IUserConfigurationRepository, UserConfigurationRepository>();

            services.AddSingleton<IUserManager, UserManager>();
            services.AddSingleton<ISocketManager, Services.SocketManager>();
            services.AddSingleton<IProvider, Provider>();
            services.AddSingleton<IChannelManager, ChannelManager>();
            services.AddSingleton<IStorage, Storage>();
            services.AddScoped<IUserService, UserService>();
            

            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseSqlite(Settings.SqliteConnectionString);
            });

            services.AddMvc()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseSession();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });
            app.UseCors(builder => builder.WithOrigins("http://127.0.0.1:8080", "http://localhost:8080").AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            
            app.UseCookieAuthentication(Options.AuthenticationOptions.CookieAuthenticationOptions(Settings.RedisConnectionString));

            app.UseMicrosoftAccountAuthentication(OAuthProviderOptions.MicrosoftOAuthOptions(Settings.Authentication.Microsoft));
            app.UseOAuthAuthentication(OAuthProviderOptions.GitHubOAuthOptions(Settings.Authentication.GitHub));
            app.UseGoogleAuthentication(OAuthProviderOptions.GoogleOAuthOptions(Settings.Authentication.Google));

            app.UseWebSockets();
            app.UseMiddleware<UserManagerMiddleware>();
            app.UseMiddleware<SocketManagerMiddleware>();

            app.UseMvc();

            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<UserDbContext>();
                context.Database.Migrate();
            }
        }
    }
}
