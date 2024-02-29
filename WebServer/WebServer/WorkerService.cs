using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Emit;
using System.Text;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private Socket httpServer;
    private readonly HttpClient _httpClient;
    private int serverPort = 8080;
    private Thread thread;


    public WorkerService(ILogger<WorkerService> logger)
    {
        _logger = logger;
    }

    private void StartServer(CancellationToken stoppingToken)
    {
        httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
        thread = new Thread(() => ConnectionThreadMethod(stoppingToken));
        thread.Start();
    }

    private void StopServer()
    {
        try
        {
            // Close the socket
            httpServer.Close();

            // Kill the thread
            thread.Abort();
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
        // while (!stoppingToken.IsCancellationRequested)
        // { 
        //    // Thread.Sleep(100);
        // }
        //
        // StopServer();
    }

    private void ConnectionThreadMethod(CancellationToken token)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
            httpServer.Bind(endPoint);
            httpServer.Listen(1);
            _ = StartListeningForConnection(token);
        }
        catch
        {
            Console.WriteLine("Server could not start");
        }
    }

    private async Task StartListeningForConnection(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = "";
            byte[] bytes = new byte[2048];
            var client = await httpServer.AcceptAsync(token);

            // Reading the inbound connection data
            // while (!token.IsCancellationRequested)
            // {
            while (!token.IsCancellationRequested)
            {
                int numBytes = await client.ReceiveAsync(bytes, token);
               
                var tempData = Encoding.ASCII.GetString(bytes, 0, numBytes);
                data += tempData;
                if (string.IsNullOrEmpty(tempData))
                {
                    break;
                }
            }

            LogRequestData(data);
            HttpRequestModel request = new HttpRequestModel();
            request.ParseHttpRequest(data);
            request.Client = client;
            SendResponse(client);

            data = string.Empty;
            // break;

            // }
        }
    }

    private void SendResponse(Socket client)
    {
        var time = DateTime.Now;

        String resHeader =
            "HTTP/1.1 200 Everything is Fine" +
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
        client.SendTo(resData, client.RemoteEndPoint!);
        client.Close();
    }

    private void LogRequestData(string requestData)
    {
        _logger.LogInformation($"Request Data: \r\n{requestData}");
    }
}