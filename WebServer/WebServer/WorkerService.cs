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

    //Website Threads along with their should stop paramater
    private Dictionary<string, (Thread thread, bool shouldStop)> _websiteThreads =
        new Dictionary<string, (Thread, bool)>();

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();
    private readonly IHttpRequestParser _parser;
    private CancellationToken _cancellationToken;
    private readonly IMessengerService _messengerService;
    public readonly IWebsiteHostingService WebsiteHostingService;

    public WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser,
        IWebsiteHostingService websiteHostingService,
        IMessengerService messengerService)
    {
        _config = websiteHostingService.GetSettings();
        _logger = logger;
        _parser = parser;
        WebsiteHostingService = websiteHostingService;
        _messengerService = messengerService;
    }


    private void StartWebsites()
    {
        _messengerService.AddNewWebSiteAddedListener(this);
        var websites = _config.Websites;

        foreach (var website in websites)
        {
            bool shouldStop = false;
            var thread = new Thread(() => ConnectionThreadMethod(website, shouldStop));
            thread.Start();
            _websiteThreads[website.Path] = (thread, shouldStop);
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
                if (!_requestsQueue.TryDequeue(out var requestModel) || requestModel.Client == null) continue;
                _ = Task.Run(async () =>
                    {
                        var handler = requestModel.Client;
                        var website =
                            _config.Websites.FirstOrDefault(x => x.WebsitePort == requestModel.RequestedPort);
                        await handler.SendToAsync(GetResponse(requestModel, website),
                            handler.RemoteEndPoint!, stoppingToken);
                        handler.Close();
                    }, stoppingToken)
                    .ContinueWith(t => _logger.LogCritical(t.Exception, null),
                        TaskContinuationOptions.OnlyOnFaulted);
                Console.WriteLine($"website threads running:{_websiteThreads.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, null);
            }
        }

        _messengerService.RemoveWebSiteAddedListener(this);
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, bool shouldStop)
    {
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(1000);
            _logger.LogInformation($"Starting {endPoint}");
            _ = StartListeningForData(httpServer, shouldStop);
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"{website.Path} Server could not start: {ex.Message}");
            _logger.LogCritical(ex, null);
        }
    }

    private async Task StartListeningForData(Socket httpServer, bool shouldStop)
    {
        while (!shouldStop || !_cancellationToken.IsCancellationRequested)
        {
            var totalBytes = new List<byte>();
            var buffer = new byte[16_384];
            var handler = await httpServer.AcceptAsync(_cancellationToken);
            var request = new HttpRequestModel();
            var totalReceivedBytes = 0;

            while (!shouldStop || !_cancellationToken.IsCancellationRequested)
            {
                var received = await handler.ReceiveAsync(buffer, _cancellationToken);
                totalReceivedBytes += received;

                if (totalBytes.Count == 0)
                {
                    var partialData = Encoding.ASCII.GetString(buffer, 0, received);
                    request = _parser.ParseHttpRequest(partialData);
                    LogRequestData(partialData);
                }

                totalBytes.AddRange(buffer);

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
                WebsiteHostingService.LoadWebsite(totalBytes.ToArray(), request, _config);
            }

            request.Client = handler;
            _requestsQueue.Enqueue(request);
        }
    }


    private byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website)
    {
        const string statusCode = "200 OK";
        string fileName = NormalizeFileName(requestModel.Path, website.DefaultPage);
        string methodType = requestModel.RequestType;
        string requestedFile = Path.Combine(_config.RootFolder, website.Path, fileName);

        switch (methodType)
        {
            case "GET" when fileName.Equals("api/getWebsitesList"):
                return HandleGetRequest(fileName, website, statusCode);
            case "POST":
                return HandlePostRequest(fileName, website, statusCode);
            case "DELETE":
                return HandleDeleteRequest(fileName, website, statusCode);
            case "OPTIONS":
                return OptionsResponse(website);
        }

        if (!File.Exists(requestedFile))
        {
            Console.WriteLine($"File not found: {requestedFile}");
            return NotFound404(website);
        }

        return ServeFile(requestedFile, website, statusCode);
    }

    private string NormalizeFileName(string fileName, string defaultPage)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Equals("/"))
            return defaultPage;

        return fileName.TrimStart('/');
    }

    private byte[] HandleGetRequest(string fileName, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.Equals("api/getWebsitesList"))
        {
            var responseHeader = BuildHeader(statusCode, "application/json; charset=UTF-8", website);
            var websites = WebsiteHostingService.GetSettings().Websites;
            var websiteBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(websites));
            return Encoding.ASCII.GetBytes(responseHeader).Concat(websiteBytes).ToArray();
        }

        return Array.Empty<byte>();
    }

    private byte[] HandlePostRequest(string fileName, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.Equals("api/uploadWebsite"))
        {
            var responseHeader = BuildHeader(statusCode, null, website);
            return Encoding.ASCII.GetBytes(responseHeader).ToArray();
        }

        return Array.Empty<byte>();
    }

    private byte[] ServeFile(string filePath, WebsiteConfigModel website, string statusCode)
    {
        var file = File.ReadAllBytes(filePath);
        var contentType = FindContentType(filePath);
        var responseHeader = BuildHeader(statusCode, $"{contentType}; charset=UTF-8", website);
        return Encoding.ASCII.GetBytes(responseHeader).Concat(file).ToArray();
    }

    private string BuildHeader(string statusCode, string contentType, WebsiteConfigModel website)
    {
        var builder = new StringBuilder($"HTTP/1.1 {statusCode}\r\n")
            .Append("Server: Microsoft_web_server\r\n");

        if (contentType != null)
        {
            builder.Append($"Content-Type: {contentType}\r\n");
        }

        builder.Append($"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n");
        return builder.ToString();
    }

    private byte[] HandleDeleteRequest(string fileName, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.StartsWith("api/delete/website"))
        {
            var responseHeader = BuildHeader(statusCode, "application/json; charset=UTF-8", website);
            return Encoding.ASCII.GetBytes(responseHeader).ToArray();
        }

        return Array.Empty<byte>();
    }

    private byte[] OptionsResponse(WebsiteConfigModel website)
    {
        string statusCode = "200 OK";
        string responseHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            "Allow: GET, POST, OPTIONS, DELETE\r\n" +
            "Access-Control-Allow-Methods: GET, POST, OPTIONS, DELETE\r\n" +
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
        bool shouldStop = false;
        var thread = new Thread(() => { ConnectionThreadMethod(website, shouldStop); });
        thread.Start();

        // Add the thread and its stop flag to the dictionary
        //using website.Path paramater as websites id
        _websiteThreads[website.Path] = (thread, shouldStop);
    }

    public void WebSiteRemoved(WebsiteConfigModel website)
    {
        if (_websiteThreads.TryGetValue(website.Path, out var threadInfo))
        {
            // Signal the thread to stop
            _websiteThreads[website.Path] = (threadInfo.thread, true);

            // Optionally wait for the thread to finish
            threadInfo.thread.Join();

            // Remove the thread from the dictionary
            _websiteThreads.Remove(website.Path);
        }
    }
}