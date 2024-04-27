using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Net;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.CompilerServices;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;
    private Thread _thread = null!;

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;


    public readonly IWebsiteHostingService _websiteHostingService;

    public WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser, 
        IWebsiteHostingService websiteHostingService)
    {
        _config = websiteHostingService.GetSettings();
        //_serverPort = _config.Port;
        _logger = logger;
        _parser = parser;
        _websiteHostingService = websiteHostingService;
    }


    private void StartServer(CancellationToken stoppingToken)
    {
        var websites = _config.Websites;

        foreach (var website in websites)
        {
            _thread = new Thread(() => ConnectionThreadMethod(website, stoppingToken));
            _thread.Start();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        StartServer(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_requestsQueue.TryDequeue(out var requestModel) && requestModel.Client != null)
                {
                    _ = Task.Run(async () =>
                    {
                        var handler = requestModel.Client;
                        await handler.SendToAsync(GetResponse(requestModel,
                                _config.Websites.First((x) => x.WebsitePort == requestModel.RequestedPort)),
                            handler.RemoteEndPoint!, stoppingToken);
                        handler.Close();
                    }, stoppingToken)
                        .ContinueWith(t => this._logger.LogCritical(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);

                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, null);
            }
        }
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, CancellationToken token)
    {
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(10000);
            _logger.LogInformation($"Starting {endPoint}");
            _ = StartListeningForData(httpServer, token);
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"{website.Path} Server could not start: {ex.Message}");
           _logger.LogCritical(ex, null);
        }
    }
    
    

    private async Task StartListeningForData(Socket httpServer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = "";
            var totalBytes = new List<byte>();

            var bytes = new byte[8192]; 
            var handler = await httpServer.AcceptAsync(token);
            
            var totalReceivedBytes = 0;

            while (!token.IsCancellationRequested)
            {
                var received = await handler.ReceiveAsync(bytes, token);
                totalReceivedBytes += received;

                var partialData = Encoding.ASCII.GetString(bytes, 0, received);
                data += partialData;
                if (totalBytes.Count == 0)
                {
                    LogRequestData(data);
                }
                
                totalBytes.AddRange(bytes);
                // Extend totalBytes array to accommodate new data
                // Array.Resize(ref totalBytes, totalBytes.Length + received);
                 //Array.Copy(bytes, 0, totalBytes, totalBytes.Length - received, received);


                // Extend totalBytes array to accommodate new data
                // Check if the received data contains a complete message
                if (received < bytes.Length)
                {
                    // If the received data is less than the buffer size, assume it's the end of the message
                    break;
                }

                _logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
            }


            var request = _parser.ParseHttpRequest(data);


            //If request is to upload a new website
            if (request.ContentType.StartsWith("multipart/form-data;"))
            {
                _websiteHostingService.LoadWebsite(totalBytes.ToArray(), request, _config);
            }

            request.Client = handler;
            _requestsQueue.Enqueue(request);
            data = string.Empty;
        }
    }

    private byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website)
    {
        var statusCode = "200 OK";
        var fileName = requestModel.Path; // File path e.g. "/styles-XHU57CVJ.css"
        var methodType = requestModel.RequestType; // Request type e.g. GET, PUT, POST, DELETE
        var webSite = website.Path;

        //Re-routing to default page in website
        if (string.IsNullOrEmpty(fileName) || fileName.Equals("/"))
        {
            fileName = website.DefaultPage; // fileName = "index.html"
        }
        //Otherwise gets filename client wants 
        else if (fileName.StartsWith($"/"))
        {
            fileName = fileName.Substring(1);
        }


        var rootFolder = _config.RootFolder;

        var requestedFile = Path.Combine(rootFolder, webSite, fileName);

        switch (methodType)
        {
            case "GET":
                // put logic in here
                break;
            case "POST" when fileName.Equals("uploadWebsite"):
            {
                statusCode = "200 OK";
                String responseHeader =
                    $"HTTP/1.1 {statusCode}\r\n" +
                    "Server: Microsoft_web_server\r\n" +
                    $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";

                var responseData = Encoding.ASCII.GetBytes(responseHeader);
                return responseData.ToArray();
            }
            case "OPTIONS":
                return OptionsResponse(website);
        }


        // File doesn't exist, return 404 Not Found
        if (!File.Exists(requestedFile))
        {
            Console.WriteLine($"File not found: {requestedFile}");
            return NotFound404(website);
        }


        var file = File.ReadAllBytes(requestedFile);


        var contentType = FindContentType(requestedFile);

        var resHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Content-Type: {contentType}; charset=UTF-8\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";

        var resData = Encoding.ASCII.GetBytes(resHeader).Concat(file);
        return resData.ToArray();
    }

    private byte[] OptionsResponse(WebsiteConfigModel website)
    {
        string statusCode = "200 OK";
        string responseHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            "Allow: GET, POST, OPTIONS\r\n" +
            "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
            "Access-Control-Allow-Headers: Content-Type\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n" +
            "\r\n";

        var responseData = Encoding.ASCII.GetBytes(responseHeader);
        return responseData;
    }

    public static string FindContentType(string requestedFile)
    {
        switch (Path.GetExtension(requestedFile).ToLowerInvariant())
        {
            case ".js":
                return "text/javascript";
            case ".css":
                return "text/css";
            default:
                return "text/html";
        }
    }


    public static byte[] NotFound404(WebsiteConfigModel website)
    {
        var statusCode = "404 Not found";
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