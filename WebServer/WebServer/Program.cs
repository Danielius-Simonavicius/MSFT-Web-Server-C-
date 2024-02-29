using WebServer;
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

            var host = builder.Build();
            host.Run();

        }
    }
}