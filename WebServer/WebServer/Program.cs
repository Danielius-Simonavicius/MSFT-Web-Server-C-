using WebServer;
using WebServer.Services;

namespace WebServer
{
    class Program
    {
        static void Main(String[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddSingleton<IConfiguration>(configuration);
            
            builder.Services.AddTransient<IHttpRequestParser, DefaultHttpParser>();
            builder.Services.AddHostedService<WorkerService>();

            var host = builder.Build();
            host.Run();
        }
    }
}