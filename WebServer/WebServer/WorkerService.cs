using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService, IMessengerListener
{
    private readonly ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;
    private CancellationToken _cancellationToken;


    private readonly IMessengerService _messengerService;
    public readonly IWebsiteHostingService _websiteHostingService;

    public WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser,
        IWebsiteHostingService websiteHostingService,
        IMessengerService messengerService)
    {
        _config = websiteHostingService.GetSettings();
        _logger = logger;
        _parser = parser;
        _websiteHostingService = websiteHostingService;
        _messengerService = messengerService;
    }


    private void StartWebsites()
    {
        _messengerService.AddNewWebSiteAddedListener(this);
        var websites = _config.Websites;

        foreach (var website in websites)
        {
            var _thread = new Thread(() => ConnectionThreadMethod(website, _cancellationToken));
            _thread.Start();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationToken = stoppingToken;
        await Task.Yield();
        StartWebsites();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_requestsQueue.TryDequeue(out var requestModel) && requestModel.Client != null)
                {
                    var handler = requestModel.Client;
                    var website = _config.Websites.FirstOrDefault(x => x.WebsitePort == requestModel.RequestedPort);
                    await handler.SendToAsync(GetResponse(requestModel, website),
                        handler.RemoteEndPoint!, stoppingToken);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, null);
            }
        }

        _messengerService.RemoveWebSiteAddedListener(this);
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, CancellationToken token)
    {
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(1000);
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
            var totalBytes = new List<byte>();
            var buffer = new byte[16_384];
            var handler = await httpServer.AcceptAsync(token);
            var request = new HttpRequestModel();
            var totalReceivedBytes = 0;

            while (!token.IsCancellationRequested)
            {
                var received = await handler.ReceiveAsync(buffer, token);
                totalReceivedBytes += received;

                if (totalBytes.Count == 0)
                {
                    var partialData = Encoding.ASCII.GetString(buffer, 0, received);
                    request = _parser.ParseHttpRequest(partialData);
                    LogRequestData(partialData);
                }

                totalBytes.AddRange(buffer);

                // Extend totalBytes array to accommodate new data
                // Check if the received data contains a complete message
                if (received < buffer.Length)
                {
                    // If the received data is less than the buffer size, assume it's the end of the message
                    break;
                }

                _logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
            }


            //If request is to upload a new website
            if (request.ContentType.StartsWith("multipart/form-data;") && request.RequestedPort is 9090 or 4200)
            {
                _websiteHostingService.LoadWebsite(totalBytes.ToArray(), request, _config);
            }

            request.Client = handler;
            _requestsQueue.Enqueue(request);
        }
    }


    public byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website)
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
            case "GET" when fileName.Equals("api/getWebsitesList"):
                var responseHeader =
                    $"HTTP/1.1 {statusCode}\r\n" +
                    "Server: Microsoft_web_server\r\n" +
                    "Content-Type: application/json; charset=UTF-8\r\n" +
                    $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";

                var serverConfig = _websiteHostingService.GetSettings();
                var websites = serverConfig.Websites;

                // Convert the websites list to JSON
                var websiteBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(websites));
                var resData1 = Encoding.ASCII.GetBytes(responseHeader).Concat(websiteBytes);
                return resData1.ToArray();

            case "POST" when fileName.Equals("api/uploadWebsite"):
            {
                responseHeader =
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

    public void NewWebSiteAdded(WebsiteConfigModel website)
    {
        var _thread = new Thread(
            () => ConnectionThreadMethod(website, _cancellationToken));
        _thread.Start();
    }

    public void WebSiteRemoved(WebsiteConfigModel website)
    {
    }
}