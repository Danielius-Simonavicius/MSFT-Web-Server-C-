using System.Net.Sockets;

namespace WebServer.Models;

public class HttpRequestModel
{
    public string Host { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Connection { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    
    public int RequestedPort { get; set; }

    public Socket? Client { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int ContentLength { get; set; }
    public IList<KeyValuePair<string, string>> Headers { get; set; } = new List<KeyValuePair<string, string>>();
    
    public object? BodyContent { get; set; } 

}