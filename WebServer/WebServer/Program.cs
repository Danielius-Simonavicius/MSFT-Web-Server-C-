using WebServer;
using WebServer.Models;
using WebServer.Services;

namespace WebServer
{
    class Program
    {
        static void Main(String[] args)
        {

            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddTransient<IHttpRequestParser, DefaultHttpParser>();
            builder.Services.AddHostedService<WorkerService>();

            builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection("ServerConfig"));

            var host = builder.Build();
            host.Run();
        }
    }
}