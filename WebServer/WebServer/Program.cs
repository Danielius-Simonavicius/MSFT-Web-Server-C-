using WebServer;
using WebServer.Models;
using WebServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace WebServer
{
    class Program
    {
        private static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddTransient<IHttpRequestParser, DefaultHttpParser>();
            builder.Services.AddHostedService<WorkerService>();
            builder.Services.AddTransient<IWebsiteHostingService, WebsiteHostingService>();

            builder.Services.AddSingleton<IMessengerService, MessengerService>();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("WebsiteConfig.json", optional: false,
                    reloadOnChange: true) 
                .Build();

            builder.Services.AddSingleton(configuration); 
          
            var host = builder.Build();
            host.Run();
        }
    }
}