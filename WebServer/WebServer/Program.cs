using WebServer;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerService>();

var host = builder.Build();
host.Run();