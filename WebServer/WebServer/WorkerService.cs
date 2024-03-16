using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Extensions.Options;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ServerConfigModel _configModel;
    private readonly WebsiteConfigModel _webConfigModel;
    private readonly ILogger<WorkerService> _logger;
    private readonly Socket _httpServer;
    private readonly int _serverPort;
    private Thread _thread = null!;

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;

    public WorkerService(ILogger<WorkerService> logger, IHttpRequestParser parser, IOptions<ServerConfigModel> config)
    {
        _configModel = config.Value;
        _serverPort = _configModel.Port;
        _logger = logger;
        _httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _parser = parser;
    }


    private void StartServer(CancellationToken stoppingToken)
    {
        _thread = new Thread(() => ConnectionThreadMethod(stoppingToken));
        _thread.Start();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var rootFolder = _configModel.RootFolder;
        StartServer(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            
            if (_requestsQueue.TryDequeue(out var requestModel) 
                && requestModel.Client != null)
            {
                var handler = requestModel.Client;
                await handler.SendToAsync(GetResponse(requestModel.Path,_configModel.WebsiteConfig.First((x) => x.IsDefault)), handler.RemoteEndPoint!, stoppingToken);
                handler.Close();
            }

            Thread.Sleep(100);
        }
    }

    private void ConnectionThreadMethod(CancellationToken token)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, _serverPort);
            _httpServer.Bind(endPoint);
            _httpServer.Listen(100);
            _ = StartListeningForData(token);
        }
        catch
        {
            Console.WriteLine("Server could not start");
        }
    }

    private async Task StartListeningForData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = "";
            var bytes = new byte[1_024];
            var handler = await _httpServer.AcceptAsync(token);

            while (!token.IsCancellationRequested)
            {
                var received = await handler.ReceiveAsync(bytes, token);
                var partialData = Encoding.ASCII.GetString(bytes, 0, received);
                data += partialData;

                if (data.Contains("\r\n"))
                {
                    break;
                }
            }

            LogRequestData(data);
            var request = _parser.ParseHttpRequest(data);
            request.Client = handler;
            _logger.LogInformation($"About to sent response to {handler.RemoteEndPoint}");
            _requestsQueue.Enqueue(request);


            data = string.Empty;
        }
    }
    
    private byte[] GetResponse(string path, WebsiteConfigModel website)
    {
        
        string statusCode = "200 OK";
        string fileName = path;
        
        if (string.IsNullOrEmpty(fileName) || fileName.Equals("/"))
        {
            fileName = website.DefaultPage;
        }
        else if (fileName.StartsWith($"/"))
        {
            fileName = fileName.Substring(1);
        }

        var rootFolder = _configModel.RootFolder;

        var requestedFile = Path.Combine(rootFolder, website.Path, fileName);
        
        // File doesn't exist, return 404 Not Found
        if (!File.Exists(requestedFile))
        {
            Console.WriteLine("File not found!");
            return BadRequest404(website,statusCode);
        }
        
        
        var file = File.ReadAllBytes(requestedFile);
        
        
        //TODO: is there better way to detect??
        string contentType = "text/html";
        if (requestedFile.EndsWith(".js"))
        {
            contentType = "text/javascript";
        }else if (requestedFile.EndsWith(".css"))
        {
            contentType = "text/css";
        }
        
        String resHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Content-Type: {contentType}; charset=UTF-8\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";
        
       var resData = Encoding.ASCII.GetBytes(resHeader).Concat(file);
        return resData.ToArray();

    }

    public byte[] BadRequest404(WebsiteConfigModel website,string statusCode)
    {
        statusCode = "404 Not found";
        String responseHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";
        var responseData = Encoding.ASCII.GetBytes(responseHeader);
        return responseData.ToArray();
    }

   

    private void LogRequestData(string requestData)
    {
        _logger.LogInformation($"\n{requestData}");
    }
}