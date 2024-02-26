using System.Net.Sockets;
using System.Net;
using System.Text;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private Socket httpServer;
    private int serverPort = 8080;
    private Thread thread;


    public WorkerService(ILogger<WorkerService> logger)
    {
        _logger = logger;
    }

    private void StartServer()
    {
        httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
        thread = new Thread(new ThreadStart(this.ConnetionThreadMethod));
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
        StartServer();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await Task.Delay(1000, stoppingToken);
        }
        StopServer();
    }

    private void ConnetionThreadMethod()
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
            httpServer.Bind(endPoint);
            httpServer.Listen(1);
            StartListeningForConnection();
        }
        catch
        {
            Console.WriteLine("I could not start");
        }
    }

    private void StartListeningForConnection()
    {
        while (true)
        {
            DateTime time = DateTime.Now;
            String data = "";
            byte[] bytes = new byte[2048];
            Socket client = httpServer.Accept(); //blocking statement

            // Reading the inbound connection data
            while (true)
            {
                int numBytes = client.Receive((bytes));
                data += Encoding.ASCII.GetString(bytes, 0, numBytes);
                if (data.IndexOf("\r\n") > -1)
                {
                    break;
                }
            }

            // Data Read
            String resHeader =
                "HTTP/1.1 200 Everything is Fine" +
                "\nServer: Microsoft_web_server" +
                "\nContent-Type: text/html; charset: UTF-8\n\n";
            
                String resBody = "<!DOCTYE html> " +
                             "<html>" +
                             "<head><title>Microsoft Server</title></head>" +
                             "<body>" +
                             "<h4>Server Time is: " + time + " </h4>" +
                             "</body></html>";

            String resStr = resHeader + resBody;

            byte[] resData = Encoding.ASCII.GetBytes(resStr);

            client.SendTo(resData, client.RemoteEndPoint);
            client.Close();
        }
    }

    // public Task StopAsync(CancellationToken cancellationToken)
    // {
    //     _logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
    //     return Task.CompletedTask;
    // }
}