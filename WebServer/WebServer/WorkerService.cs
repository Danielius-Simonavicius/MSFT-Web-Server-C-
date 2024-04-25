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
    public readonly WebsiteParser _fileParser = new WebsiteParser();

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
            try
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
            catch (Exception ex)
            {
                _logger.LogError($"{ex}");
            }
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

    private async Task StartListeningForData(Socket httpServer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = "";
            byte[] totalBytes = new byte[0];

            var bytes = new byte[1024]; //102400
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
                }
                
                // Extend totalBytes array to accommodate new data
                Array.Resize(ref totalBytes, totalBytes.Length + received);
                Array.Copy(bytes, 0, totalBytes, totalBytes.Length - received, received);

                // Check if the received data contains a complete message
                if (received < bytes.Length)
                {
                    // If the received data is less than the buffer size, assume it's the end of the message
                    //LogRequestData(totalBytes);
                    break;
                }

                _logger.LogInformation($"Total MB received: {totalReceivedBytes / (1024 * 1024)}");
            }
        


            var request = _parser.ParseHttpRequest(data);

            if (request.ContentType.StartsWith("multipart/form-data;"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(request.ContentType,
                    @"boundary=(?<boundary>.+)");
                string boundary = match.Success ? match.Groups["boundary"].Value.Trim() : "";

                byte[] delimiter = Encoding.ASCII.GetBytes("--" + boundary);


                // Split the byte array
                List<byte[]> parts = SplitByteArray(bytes, delimiter);
                string[] stringArray = parts.ConvertAll(bytes => Encoding.ASCII.GetString(bytes)).ToArray();

                // Extract file content from parts[1]
                byte[] fileContent = ExtractFileContent(parts[0]);
                _fileParser.ExtractWebsiteFile(fileContent);


                request.Client = handler;
            }
            
            _requestsQueue.Enqueue(request);


            data = string.Empty;
        }
    }

    static byte[] ExtractFileContent(byte[] byteArray)
    {
        string pkSignature = "PK";
        byte[] pkBytes = Encoding.ASCII.GetBytes(pkSignature);
        int index = IndexOf(byteArray, pkBytes, 0);
        if (index != -1)
        {
            // Return the file content byte array starting from "PK"
            return SubArray(byteArray, index, byteArray.Length - index);
        }
        else
        {
            // Handle case when "PK" is not found
            return null; // or throw an exception
        }
    }

    static List<byte[]> SplitByteArray(byte[] byteArray, byte[] delimiter)
    {
        List<byte[]> parts = new List<byte[]>();
        int delimiterIndex = IndexOf(byteArray, delimiter, 0);

        while (delimiterIndex != -1)
        {
            parts.Add(SubArray(byteArray, 0, delimiterIndex));
            byteArray = SubArray(byteArray, delimiterIndex + delimiter.Length,
                byteArray.Length - delimiterIndex - delimiter.Length);
            delimiterIndex = IndexOf(byteArray, delimiter, 0);
        }

        if (byteArray.Length > 0)
        {
            parts.Add(byteArray);
        }

        return parts;
    }

    static int IndexOf(byte[] array, byte[] pattern, int startIndex)
    {
        for (int i = startIndex; i <= array.Length - pattern.Length; i++)
        {
            int j;
            for (j = 0; j < pattern.Length; j++)
            {
                if (array[i + j] != pattern[j])
                {
                    break;
                }
            }

            if (j == pattern.Length)
            {
                return i;
            }
        }

        return -1;
    }

    static byte[] SubArray(byte[] array, int startIndex, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(array, startIndex, result, 0, length);
        return result;
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