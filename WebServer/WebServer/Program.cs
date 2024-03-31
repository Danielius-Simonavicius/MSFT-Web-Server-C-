using WebServer;
using WebServer.Models;
using WebServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace WebServer
{
    class Program
    {
        static void Main(String[] args)
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddTransient<IHttpRequestParser, DefaultHttpParser>();
            builder.Services.AddHostedService<WorkerService>();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Load appsettings.json
                .AddJsonFile("WebsiteConfig.json", optional: false,
                    reloadOnChange: true) // Load AdditionalConfig.json
                .Build();

            builder.Services.AddSingleton(configuration); // Add configuration to services

            // Register ServerConfigModel using configuration
            builder.Services.Configure<ServerConfigModel>(configuration.GetSection("ServerConfig"));
            // Register WebsiteListModel using configuration
            builder.Services.Configure<WebsiteListModel>(configuration.GetSection("WebsiteConfigList"));

            var host = builder.Build();
            host.Run();
            // var builder = Host.CreateApplicationBuilder(args);
            // builder.Services.AddTransient<IHttpRequestParser, DefaultHttpParser>();
            // builder.Services.AddHostedService<WorkerService>();
            // builder.Services.Configure<ServerConfigModel>(builder.Configuration.GetSection("ServerConfig"));
            // var host = builder.Build();
            // host.Run();
        }
    }
}