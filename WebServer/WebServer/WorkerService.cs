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

    private void StopServer()
    {
        try
        {
            // Close the socket
            _httpServer.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Stopping failed");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var rootFolder = _configModel.RootFolder;
        StartServer(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var request = _requestsQueue.TryDequeue(out var requestModel) ? requestModel : null;
            if (request != null && request.Client != null)
            {
                var handler = request.Client;
                await handler.SendToAsync(GetResponse(), handler.RemoteEndPoint!, stoppingToken);
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
    
    private byte[] GetResponse()
    {
        var time = DateTime.Now;

        String resHeader =
            "HTTP/1.1 200 OK\r\n" +
            "Server: Microsoft_web_server\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "Access-Control-Allow-Origin: *\r\n\r\n";

        String resBody = "<!DOCTYPE html> " +
                         "<html>" +
                         "<head><title>Microsoft Server</title></head>" +
                         "<body>" +
                         $"<h4>Server Time is: {time} </h4>" +
                         "</body></html>";
        
        String resStr = resHeader + resBody;
        
        byte[] resData = Encoding.ASCII.GetBytes(resStr);
        return resData;

    }

   

    private void LogRequestData(string requestData)
    {
        _logger.LogInformation($"\n{requestData}");
    }
}