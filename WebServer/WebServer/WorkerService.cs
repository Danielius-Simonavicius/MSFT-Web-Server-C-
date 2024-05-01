using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser,
        IWebsiteHostingService websiteHostingService,
        IMessengerService messengerService,
        IGetResponseService responseService)
    : BackgroundService, IMessengerListener
{
    private readonly ServerConfigModel _config = websiteHostingService.GetSettings();

    //Website Threads along with their should stop paramater
    private Dictionary<string, (Thread thread, bool shouldStop)> _websiteThreads = new();

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();
    private CancellationToken _cancellationToken;
    public readonly IWebsiteHostingService WebsiteHostingService = websiteHostingService;


    private void StartWebsites()
    {
        messengerService.AddNewWebSiteAddedListener(this);
        var websites = _config.Websites;

        foreach (var website in websites)
        {
            bool shouldStop = false;
            var thread = new Thread(() => ConnectionThreadMethod(website, shouldStop));
            thread.Start();
            _websiteThreads[website.WebsiteId] = (thread, shouldStop);
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
                        await handler.SendToAsync(responseService.GetResponse(requestModel, website,_config), handler.RemoteEndPoint!, stoppingToken);
                        handler.Close();
                    }, stoppingToken)
                    .ContinueWith(t => logger.LogCritical(t.Exception, null),
                        TaskContinuationOptions.OnlyOnFaulted);
                logger.LogInformation($"\r\nwebsite threads running:{_websiteThreads.Count}");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, null);
            }
        }

        messengerService.RemoveWebSiteAddedListener(this);
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, bool shouldStop)
    {
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(1000);
            logger.LogInformation($"Starting {endPoint}");
            _ = StartListeningForData(httpServer, shouldStop);
        }
        catch (Exception ex)
        {
            logger.LogCritical($"{website.Path} Server could not start: {ex.Message}");
            logger.LogCritical(ex, null);
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
                    request = parser.ParseHttpRequest(partialData);
                    LogRequestData(partialData);
                }

                totalBytes.AddRange(buffer);

                // Check if the received data contains a complete message
                if (received < buffer.Length)
                {
                    // If the received data is less than the buffer size, assume it's the end of the message
                    break;
                }

                logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
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
        logger.LogInformation($"\n{parts[0]}");
    }

    public void NewWebSiteAdded(WebsiteConfigModel website)
    {
        bool shouldStop = false;
        var thread = new Thread(() => ConnectionThreadMethod(website, shouldStop));
        thread.Start();

        // Add the thread and its stop flag to the dictionary
        _websiteThreads[website.WebsiteId] = (thread, shouldStop);
    }

    public void WebSiteRemoved(WebsiteConfigModel website)
    {
        if (_websiteThreads.TryGetValue(website.WebsiteId, out var threadInfo))
        {
            // Signal the thread to stop
            _websiteThreads[website.WebsiteId] = (threadInfo.thread, true);
            
            //Delete website folder
            var pathToWebsite = Path.Combine(_config.RootFolder, website.WebsiteId);
            Directory.Delete(pathToWebsite,true);
            
            // Optionally wait for the thread to finish
            threadInfo.thread.Join();
            //threadInfo.thread.Abort();
            // Remove the thread from the dictionary
            _websiteThreads.Remove(website.WebsiteId);
        }
    }
}