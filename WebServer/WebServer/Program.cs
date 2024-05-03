using WebServer;
using WebServer.Models;
using WebServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebServer
{
    class Program
    {
        private static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddTransient<IHttpRequestParser, DefaultHttpParser>();
            builder.Services.AddTransient<IWebsiteHostingService, WebsiteHostingService>();
            
            builder.Services.AddTransient<IGetResponseService, DefaultResponseService>();
            builder.Services.AddTransient<IConfigurationService, DefaultConfigurationService>();
            builder.Services.AddHostedService<WorkerService>();

            builder.Services.AddSingleton<IMessengerService, MessengerService>();
            // var configuration = new ConfigurationBuilder()
            //     .AddJsonFile("WebsiteConfig.json", optional: false,
            //         reloadOnChange: true) 
            //     .Build();
            // builder.Services.AddSingleton(configuration); 
          
            var host = builder.Build();
            host.Services.GetRequiredService<IOptionsMonitor<ServerConfigModel>>();
            
            host.Run();
        }
    }
}