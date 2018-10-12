using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bot;
using Bot.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemiBot;

namespace Bot_Builder_Echo_Bot_V4
{

    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private bool _isProduction = false;

        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(c => c.UseMemoryStorage());

            IStorage dataStore = new Microsoft.Bot.Builder.MemoryStorage();

            var jobState = new JobState(dataStore);

            services.AddSingleton(sp => jobState);
            services.AddSingleton(gs => new RemiBotGeneratorService());

            services.AddBot<RemiBot>(options =>
            {
                var secretKey = Configuration.GetSection("botFileSecret")?.Value;
                var botFilePath = Configuration.GetSection("botFilePath")?.Value;

                var botConfig = BotConfiguration.Load(botFilePath ?? @".\BotConfiguration.bot", secretKey);
                services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

                var environment = _isProduction ? "production" : "development";
                var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == environment).FirstOrDefault();
                if (!(service is EndpointService endpointService))
                {
                    throw new InvalidOperationException($"The .bot file does not contain an endpoint with name '{environment}'.");
                }

                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                ILogger logger = _loggerFactory.CreateLogger<RemiBot>();

                options.OnTurnError = async (context, exception) =>
                {
                    logger.LogError($"Exception caught : {exception}");
                    await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                };

                var conversationState = new ConversationState(dataStore);
                options.State.Add(conversationState);

                var userState = new UserState(dataStore);
                options.State.Add(userState);
            });

            services.AddSingleton(sp =>
            {
                var config = BotConfiguration.Load(@".\BotConfiguration.bot");
                var endpointService = (EndpointService)config.Services.First(s => s.Type == "endpoint")
                                        ?? throw new InvalidOperationException(".bot file 'endpoint' must be configured prior to running.");

                return endpointService;
            });

            services.AddSingleton<RemiBotAccessors>(sp => 
            {
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();
                var userState = options.State.OfType<UserState>().FirstOrDefault();

                var accessors = new RemiBotAccessors(conversationState, userState)
                { 
                    TopicStateAccessor = conversationState.CreateProperty<TopicState>(RemiBotAccessors.TopicStateName),
                    UserProfileAccessor = userState.CreateProperty<UserProfile>(RemiBotAccessors.UserProfileName),
                    ConversationDialogState = conversationState.CreateProperty<DialogState>(RemiBotAccessors.CondversationDialogStateName),
                };

                return accessors;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();

            app.UseHangfireServer();
        }
    }
}
