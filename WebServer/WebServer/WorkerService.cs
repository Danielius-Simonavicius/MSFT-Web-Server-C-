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
using System;
using System.Net.Security;
using System.Security.Cryptography;
using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;
    private Thread _thread = null!;

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
                var hostParts =
                    requestModel.Host.Split(":"); //hostParts = localhost:8085 (trying to find port e.g. "8085")
                int port = IntegerType.FromString(hostParts[1]);
                await handler.SendToAsync(GetResponse(requestModel,
                    _config.Websites.First((x) => x.WebsitePort == port)), handler.RemoteEndPoint!, stoppingToken);
                handler.Close();
            }

            Thread.Sleep(100);
        }
    }

    private async Task ConnectionThreadMethod(WebsiteConfigModel website, CancellationToken token)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            //var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //httpServer.Bind(endPoint);
            //httpServer.Listen(100);
            TcpListener TCPListener = new TcpListener(endPoint);
            TCPListener.Start();
            while (!token.IsCancellationRequested)
            {
                TcpClient client = await TCPListener.AcceptTcpClientAsync();
                _ = ProcessClientAsync(client, token);
            }
            //_ = StartListeningForData(httpServer, token);
            //var handler = new HttpClientHandler();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{website.Path} Server could not start: {ex.Message}");
        }
    }


    private async Task ProcessClientAsync(TcpClient client, CancellationToken token)
    {
        X509Certificate2 serverCertificate = new X509Certificate2(
            "/Users/danieljr/Desktop/Projects/MSFT-Web-Server-C-/WebServer/WebServer/certificates/MSFTServer.pfx",
            "microsoftProject");

        using (SslStream sslStream = new SslStream(client.GetStream(), false))
        {
            try
            {
                await sslStream.AuthenticateAsServerAsync(serverCertificate, false, SslProtocols.Tls12, false);
                Console.WriteLine("Server authenticated");
                await StartListeningForData(sslStream, token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SSL/TLS handshake failed: {ex.Message}");

                // Optionally, you can handle specific exceptions and provide more detailed error messages
                if (ex is AuthenticationException authEx)
                {
                    // Log the authentication failure
                    _logger.LogError($"Authentication failed: {authEx.Message}");
                }
                else if (ex is IOException ioEx)
                {
                    // Log I/O related errors
                    _logger.LogError($"I/O error: {ioEx.Message}");
                }
                else
                {
                    // Log other types of exceptions
                    _logger.LogError($"An error occurred: {ex.Message}");
                }

                // Optionally, you can rethrow the exception or handle it gracefully based on your application's requirements
                throw;
            }
        }
    }


    // Proceed with secure communication

    /*catch (AuthenticationException e)
    {
        Console.WriteLine($"Server authentication failed: {e.Message}");
        // Handle authentication failure
    }*/


    private async Task StartListeningForData(SslStream sslStream, CancellationToken token)
    {
        //X509Certificate2 serverCertificate = new X509Certificate2("D:\\MicrosoftProj\\MSFT-Web-Server\\WebServer\\WebServer\\Files\\MSFTServer.pfx","microsoftProject");
        //X509Certificate2 clientCertificate = new X509Certificate2("D:\\MicrosoftProj\\MSFT-Web-Server\\WebServer\\WebServer\\Files\\clientca+key.pfx","microsoftProject");
        //handler.ClientCertificates.Add(clientCertificate );
        //var handler = await httpServer.AcceptAsync(token);
        //X509Certificate2 cert = GetCertificateFromStore("CN=CERT_SIGN_TEST_CERT");

        //SslStream sslStream = new SslStream(new NetworkStream(handler), false);
        try
        {
            while (!token.IsCancellationRequested)
            {
                var data = "";
                byte[] bytes = new byte[1024];
                //int bytesRead;

                while (!token.IsCancellationRequested)
                {
                    var received = await sslStream.ReadAsync(bytes, 0, bytes.Length);
                    var partialData = Encoding.ASCII.GetString(bytes, 0, received);

                    //string partialData = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                    data += partialData;

                    if (data.Contains("\r\n"))
                    {
                        break;
                    }
                }

                // End of HTTP request detected, process the request
                LogRequestData(data);
                var request = _parser.ParseHttpRequest(data);
                // Since you're not sending responses, you don't need to set request.Client
                _requestsQueue.Enqueue(request);

                // Reset data for next request
                data = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in secure communication: {ex.Message}");
        }
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

        if (methodType.Equals("GET")) // Do we sort methods in here like this? one after the other?
        {
            // put logic in here
        }

        if (methodType.Equals("POST"))
        {
            //put logic in here
        }


        // File doesn't exist, return 404 Not Found
        if (!File.Exists(requestedFile))
        {
            Console.WriteLine($"File not found: {requestedFile}");
            return NotFound404(website, statusCode);
        }


        var file = File.ReadAllBytes(requestedFile);


        //TODO: is there better way to detect??
        string contentType = FindContentType(requestedFile);

        String resHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Content-Type: {contentType}; charset=UTF-8\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";

        var resData = Encoding.ASCII.GetBytes(resHeader).Concat(file);
        return resData.ToArray();
    }

    public static string FindContentType(string requestedFile) =>
        requestedFile.EndsWith(".js") ? "text/javascript" :
        requestedFile.EndsWith(".css") ? "text/css" :
        "text/html";


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