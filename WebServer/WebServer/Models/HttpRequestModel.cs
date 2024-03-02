using System.Net.Sockets;

namespace WebServer.Models;

public class HttpRequestModel
{
    public string Host { get; set; } = string.Empty;
    public string MethodType { get; set; } = string.Empty;
    public string Connection { get; set; } = string.Empty;

    public Socket? Client { get; set; }

    public IList<KeyValuePair<string, string>> Headers { get; set; } = new List<KeyValuePair<string, string>>();

}