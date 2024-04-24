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
using WebServer.WebSites;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;
    private Thread _thread = null!;
    private WebsiteParser _fileParser = new WebsiteParser();

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;

    public WorkerService(ILogger<WorkerService> logger,
        IHttpRequestParser parser, IOptions<ServerConfigModel> config)
    {
        _config = config.Value;
        //_serverPort = _config.Port;
        _logger = logger;
        _parser = parser;
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
            if (_requestsQueue.TryDequeue(out var requestModel) && requestModel.Client != null)
            {
                var handler = requestModel.Client;
                await handler.SendToAsync(GetResponse(requestModel,
                        _config.Websites.First((x) => x.WebsitePort == requestModel.RequestedPort)),
                    handler.RemoteEndPoint!, stoppingToken);
                handler.Close();
            }

            Thread.Sleep(100);
        }
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, CancellationToken token)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(100);
            _ = StartListeningForData(httpServer, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{website.Path} Server could not start: {ex.Message}");
        }
    }


    // private async Task StartListeningForData(Socket httpServer, CancellationToken token)
    // {
    //     try
    //     {
    //         while (!token.IsCancellationRequested)
    //         {
    //             // Accept incoming connection asynchronously
    //             Socket handler = await httpServer.AcceptAsync();
    //
    //             // Start processing the received data asynchronously
    //             _ = ProcessDataAsync(handler, token);
    //         }
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         // Handle cancellation gracefully
    //     }
    //     catch (Exception ex)
    //     {
    //         // Handle other exceptions
    //         Console.WriteLine($"An error occurred: {ex.Message}");
    //     }
    //     finally
    //     {
    //         // Clean up resources
    //         httpServer.Close();
    //     }
    //     
    // }


    private async Task StartListeningForData(Socket httpServer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = "";
            var bytes = new byte[4096]; //102400
            var handler = await httpServer.AcceptAsync(token);
            var totalReceivedBytes = 0;

            while (!token.IsCancellationRequested)
            {
                var received = await handler.ReceiveAsync(bytes, token);
                totalReceivedBytes += received;

                var partialData = Encoding.ASCII.GetString(bytes, 0, received);
                data += partialData;
                if (data.Contains("\r\n"))
                {
                    LogRequestData(data);
                    break;
                }
            }

            var request = _parser.ParseHttpRequest(data);
            var expectedBytes = request.ContentLength;


            /*checks if client is uploading from-data object from front end. This will parse file bytes,
             extract and place newly uploaded website into website directory*/
            //todo var partialBytes = new byte[4096]; // Chunk size
            var fileBytes = new byte[request.ContentLength];

            if (request.ContentType.StartsWith("multipart/form-data;"))
            {
                while (!token.IsCancellationRequested && (totalReceivedBytes <= expectedBytes))
                {
                    var received = await handler.ReceiveAsync(fileBytes, token);
                    totalReceivedBytes += received;
                    var partialData = Encoding.ASCII.GetString(fileBytes, 0, received);
                    data += partialData;
                    _logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
                }
                string test = data;
                var listBytes = new List<byte>();
                foreach (var b in bytes)
                {
                    listBytes.Add(b);
                }
                foreach (var b in fileBytes)
                {
                    listBytes.Add(b);
                }

                var fullArray = listBytes.ToArray();
            
                //Finding the boundary's in the byte input for the zip file
                var match = System.Text.RegularExpressions.Regex.Match(request.ContentType,
                    @"boundary=(?<boundary>.+)");
                string boundary = match.Success ? match.Groups["boundary"].Value.Trim() : "";

                byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary);
                
                // Find the index of the first occurrence of the starting boundary in the dataBytes array
                var startIndex = FindBoundaryIndex(fullArray, boundaryBytes);

                // Find the index of the next occurrence of the ending boundary in the dataBytes array
                var endIndex = FindBoundaryIndex(fullArray, boundaryBytes,
                    startIndex + boundaryBytes.Length);

                // Extract the content between the boundaries
                var contentBetweenBoundaries = fullArray.Skip(startIndex + boundaryBytes.Length)
                    .Take(endIndex - startIndex - boundaryBytes.Length).ToArray();

                //Extracting zip file (website)
                byte[] zipData = _fileParser.ExtractBinaryData(contentBetweenBoundaries, Encoding.ASCII.GetBytes("Content-Type: application/zip\r\n\r\n"));
                _fileParser.ExtractWebsiteFile(zipData);

            }
            
            request.Client = handler;
            _requestsQueue.Enqueue(request);


            data = string.Empty;
        }
    }
    // Define a method to find the boundary index in a byte array
    public static int FindBoundaryIndex(byte[] data, byte[] boundary, int startIndex = 0)
    {
        for (var i = startIndex; i < data.Length - boundary.Length; i++)
        {
            var isBoundary = !boundary.Where((t, j) => data[i + j] != t).Any();
            if (isBoundary)
            {
                return i;
            }
        }
        return -1; // Boundary not found
    }

    private byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website)
    {
        string statusCode = "200 OK";
        string fileName = requestModel.Path; // File path e.g. "/styles-XHU57CVJ.css"
        string methodType = requestModel.RequestType; // Request type e.g. GET, PUT, POST, DELETE
        string webSite = website.Path;

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

        if (methodType.Equals("GET"))
        {
            // put logic in here
        }
        else if (methodType.Equals("POST") && fileName.Equals("uploadWebsite"))
        {
            statusCode = "200 OK";
            String responseHeader =
                $"HTTP/1.1 {statusCode}\r\n" +
                "Server: Microsoft_web_server\r\n" +
                $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";

            var responseData = Encoding.ASCII.GetBytes(responseHeader);
            return responseData.ToArray();
        }
        else if (methodType.Equals("OPTIONS"))
        {
            return OptionsResponse(website);
        }


        // File doesn't exist, return 404 Not Found
        if (!File.Exists(requestedFile))
        {
            Console.WriteLine($"File not found: {requestedFile}");
            return NotFound404(website, statusCode);
        }


        var file = File.ReadAllBytes(requestedFile);


        string contentType = FindContentType(requestedFile);

        String resHeader =
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


    public byte[] NotFound404(WebsiteConfigModel website, string statusCode)
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