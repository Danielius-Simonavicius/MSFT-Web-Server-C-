using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService, IMessengerListener
{
 
    private Dictionary<string, CancellationTokenSource> _websiteThreads = new();
    private readonly ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;
    private CancellationToken _cancellationToken;
    private readonly IGetResponseService _responseService;

    private readonly IMessengerService _messengerService;
    public readonly IWebsiteHostingService WebsiteHostingService;

    public WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser,
        IWebsiteHostingService websiteHostingService,
        IMessengerService messengerService, IGetResponseService responseService)
    {
        _config = websiteHostingService.GetSettings();
        _logger = logger;
        _parser = parser;
        WebsiteHostingService = websiteHostingService;
        _messengerService = messengerService;
        _responseService = responseService;
    }

    private void StartWebsites()
    {
        _messengerService.AddNewWebSiteAddedListener(this);
        var websites = _config.Websites;

        foreach (var website in websites)
        {
            var threadCancellationToken = new CancellationTokenSource();
            var thread = new Thread(() => ConnectionThreadMethod(website, threadCancellationToken));
            thread.Start();
            //Adds to dictionary of threads
            _websiteThreads[website.WebsiteId] = threadCancellationToken;
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
                        var website = _config.Websites.FirstOrDefault(x => x.WebsitePort == requestModel.RequestedPort);
                        await handler.SendToAsync(_responseService.GetResponse(requestModel, website!, _config),
                            handler.RemoteEndPoint!, stoppingToken);
                        handler.Close();
                    }, stoppingToken)
                    .ContinueWith(t => _logger.LogCritical(t.Exception, null),
                        TaskContinuationOptions.OnlyOnFaulted);
                _logger.LogInformation($"\r\nwebsite threads running:{_websiteThreads.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, null);
            }
        }

        _messengerService.RemoveWebSiteAddedListener(this);
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, CancellationTokenSource threadCancellationToken)
    {
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(1000);
            _logger.LogInformation($"Starting {endPoint}");
            _ = StartListeningForData(httpServer, threadCancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"{website.Path} Server could not start: {ex.Message}");
            _logger.LogCritical(ex, null);
        }
    }

    private async Task StartListeningForData(Socket httpServer, CancellationTokenSource threadCancellationToken)
    {
        while (!threadCancellationToken.IsCancellationRequested)
        {
            var totalBytes = new List<byte>();
            var buffer = new byte[16_384];
            var handler = await httpServer.AcceptAsync(_cancellationToken);
            var request = new HttpRequestModel();
            var totalReceivedBytes = 0;

            while (!threadCancellationToken.IsCancellationRequested)
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

    private void LogRequestData(string requestData)
    {
        var parts = requestData.Split("\r\n\r\n");
        _logger.LogInformation($"\n{parts[0]}");
    }

    public void NewWebSiteAdded(WebsiteConfigModel website)
    {
        var threadCancellationToken = new CancellationTokenSource();
        var thread = new Thread(() => ConnectionThreadMethod(website, threadCancellationToken));
        thread.Start();

        // Add the thread and its stop flag to the dictionary
        _websiteThreads[website.WebsiteId] = threadCancellationToken;
    }

    public void WebSiteRemoved(WebsiteConfigModel website)
    {
        if (_websiteThreads.TryGetValue(website.WebsiteId, out var cancellationToken))
        {
            // Signal the thread to stop
            cancellationToken.Cancel();
            _websiteThreads.TryGetValue(website.WebsiteId, out var threadInfo);
            
            //Delete website folder
            var pathToWebsite = Path.Combine(_config.RootFolder, website.WebsiteId);
            Directory.Delete(pathToWebsite, true);


            // Remove the thread from the dictionary
            _websiteThreads.Remove(website.WebsiteId);
        }
    }
}