using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly Socket httpServer;
    private readonly int serverPort = 8080;
    private Thread thread = null!;

    private readonly ConcurrentQueue<HttpRequestModel> RequestsQueue = new();

    private readonly IHttpRequestParser _parser;

    public WorkerService(ILogger<WorkerService> logger, IHttpRequestParser parser)
    {
        _logger = logger;
        httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _parser = parser;
    }

    private void StartServer(CancellationToken stoppingToken)
    {
        thread = new Thread(() => ConnectionThreadMethod(stoppingToken));
        thread.Start();
    }

    private void StopServer()
    {
        try
        {
            // Close the socket
            httpServer.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Stopping failed");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        StartServer(stoppingToken);
         while (!stoppingToken.IsCancellationRequested)
         { 
        
             var request = RequestsQueue.TryDequeue(out var requestModel) ? requestModel : null;
             if (request != null 
                 && request.Client != null)
             {
                 var handler = request.Client;
                  await handler.SendToAsync(GetResponse(), handler.RemoteEndPoint!, stoppingToken);
                 //            handler.Close();
             }
             Thread.Sleep(100);
         }
        
        // StopServer();
    }

    private void ConnectionThreadMethod(CancellationToken token)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
            httpServer.Bind(endPoint);
            httpServer.Listen(100);
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
            byte[] bytes = new byte[1_024];
            var handler = await httpServer.AcceptAsync(token);

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
            RequestsQueue.Enqueue(request);
           
            
            data = string.Empty;
            // break;

            // }
        }
    }

    private byte[] GetResponse()
    {
        var time = DateTime.Now;

        String resHeader =
            "HTTP/1.1 200" +
            "\nServer: Microsoft_web_server" +
            "\nContent-Type: text/html; charset: UTF-8\n\n";

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
        _logger.LogInformation($"Request Data: \r\n{requestData}");
    }
}