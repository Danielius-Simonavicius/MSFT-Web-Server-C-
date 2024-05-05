using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService, IMessengerListener
{
    private readonly Dictionary<string, CancellationTokenSource> _websiteThreads = new();
    private ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;
    private CancellationToken _cancellationToken;
    private readonly IGetResponseService _responseService;

    private readonly IMessengerService _messengerService;
    private readonly IWebsiteHostingService WebsiteHostingService;
    private readonly IConfigurationService _configurationService;

    public WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser,
        IWebsiteHostingService websiteHostingService,
        IMessengerService messengerService,
        IGetResponseService responseService,
        IConfigurationService configurationService
    )
    {
        _configurationService = configurationService;
        _config = _configurationService.GetSettings();
        _logger = logger;
        _parser = parser;
        WebsiteHostingService = websiteHostingService;
        _messengerService = messengerService;
        _responseService = responseService;
    }

    private void StartWebsites()
    {
        _messengerService.AddNewWebSiteAddedListener(this);
        _messengerService.AddConfigChangedListener(this);
        var websites = _config.Websites;

        foreach (var website in websites)
        {
            var threadCancellationToken = new CancellationTokenSource();
            var thread = new Thread(() => _ = ConnectionThreadMethod(website, threadCancellationToken));
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

                var handler = requestModel.Client;
                var website = _config.Websites.FirstOrDefault(x => x.WebsitePort == requestModel.RequestedPort);
                if (website == null)
                {
                    _logger.LogWarning("Got Request for invalid website on port {port}",
                        requestModel.RequestedPort);
                    return;
                }


                var requestData = _responseService.GetResponse(requestModel, website, _config);
                await handler.WriteAsync(requestData, 0, requestData.Length, stoppingToken);
                await handler.FlushAsync(stoppingToken);

                // await handler.SendToAsync(_responseService.GetResponse(requestModel, website, _config),
                //     handler.RemoteEndPoint!, stoppingToken);
                // handler.Close();

                _logger.LogInformation($"\r\nwebsite threads running:{_websiteThreads.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, null);
            }
        }

        _messengerService.RemoveWebSiteAddedListener(this);
    }

    private async Task ConnectionThreadMethod(WebsiteConfigModel website,
        CancellationTokenSource threadCancellationToken)
    {
        try
        {
            var endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);

            var tcpListener = new TcpListener(endPoint);
            tcpListener.Start();

            // var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            // httpServer.Bind(endPoint);
            // httpServer.Listen(1000);
            _logger.LogInformation($"Starting {endPoint}");
            while (!threadCancellationToken.IsCancellationRequested)
            {
                using var client = await tcpListener.AcceptTcpClientAsync(_cancellationToken);
                var tcpStream = client.GetStream();
                _ = StartListeningForData(tcpStream, threadCancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"{website.Path} Server could not start: {ex.Message}");
            _logger.LogCritical(ex, null);
        }
    }

    private async Task StartListeningForData(Stream client, CancellationTokenSource threadCancellationToken)
    {
        int readTotal;
        var totalBytes = new List<byte>();
        var buffer = new byte[2048];
        var request = new HttpRequestModel();
        var totalReceivedBytes = 0;

        //While there's data to read
        while ((readTotal = await client.ReadAsync(buffer, 0, buffer.Length, _cancellationToken)) != 0)
        {
            totalReceivedBytes += readTotal;
            //Grabbing header data
            if (totalBytes.Count == 0)
            {
                var partialData = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
                request = _parser.ParseHttpRequest(partialData);
                LogRequestData(partialData);
            }

            _logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
            totalBytes.AddRange(buffer);
        }

        //If request is to upload a new website
        if (request.ContentType.StartsWith("multipart/form-data;") && request.RequestedPort is 9090 or 4200)
        {
            _logger.LogInformation("request is to upload a new website");
            try
            {
                //todo fix this parsing logic.
                WebsiteHostingService.LoadWebsite(totalBytes.ToArray(), request);
            }
            catch
            {
                _logger.LogInformation("failed to load website");
            }
        }

        request.Client = client;
        _requestsQueue.Enqueue(request);

        // while (!threadCancellationToken.IsCancellationRequested)
        // {
        //     var totalBytes = new List<byte>();
        //     var buffer = new byte[8192];
        //     var request = new HttpRequestModel();
        //     var totalReceivedBytes = 0;
        //
        //     while (!threadCancellationToken.IsCancellationRequested)
        //     {
        //         var received = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken);
        //         totalReceivedBytes += received;
        //
        //         if (received == 0) // Check if the socket has been closed
        //         {
        //             _logger.LogWarning("Socket closed by client");
        //             break;
        //         }
        //
        //         if (totalBytes.Count == 0)
        //         {
        //             var partialData = Encoding.ASCII.GetString(buffer, 0, received);
        //             request = _parser.ParseHttpRequest(partialData);
        //             LogRequestData(partialData);
        //         }
        //
        //         totalBytes.AddRange(buffer);
        //
        //         // Check if the received data contains a complete message
        //         if (received < buffer.Length)
        //         {
        //             // If the received data is less than the buffer size, assume it's the end of the message
        //             break;
        //         }
        //
        //         _logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
        //     }
        //
        //     //If request is to upload a new website
        //     if (request.ContentType.StartsWith("multipart/form-data;") && request.RequestedPort is 9090 or 4200)
        //     {
        //         _logger.LogInformation("request is to upload a new website");
        //         try
        //         {
        //             //todo fix this parsing logic.
        //             WebsiteHostingService.LoadWebsite(totalBytes.ToArray(), request);
        //         }
        //         catch
        //         {
        //             _logger.LogInformation("failed to load website");
        //         }
        //     }
        //
        //     request.Client = stream;
        //     _requestsQueue.Enqueue(request);
        // }
    }

    private void LogRequestData(string requestData)
    {
        var parts = requestData.Split("\r\n\r\n");
        _logger.LogInformation($"\n{parts[0]}");
    }

    public void NewWebSiteAdded(WebsiteConfigModel website)
    {
        _config = this._configurationService.GetSettings();
        _logger.LogInformation($"Starting Website {website.WebsiteId} thread");
        var threadCancellationToken = new CancellationTokenSource();
        var thread = new Thread(() => ConnectionThreadMethod(website, threadCancellationToken));
        thread.Start();

        // Add the thread and its stop flag to the dictionary
        _websiteThreads[website.WebsiteId] = threadCancellationToken;
    }

    public void WebSiteRemoved(WebsiteConfigModel website)
    {
        _config = this._configurationService.GetSettings();
        if (_websiteThreads.TryGetValue(website.WebsiteId, out var cancellationToken))
        {
            _logger.LogInformation($"Stopping Website {website.WebsiteId} thread");
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

    public void ConfigChanged()
    {
        _config = this._configurationService.GetSettings();
    }
}