namespace WebServer.Models;

public class ServerConfigModel
{
    public int Port { get; set; }
    public string RootFolder { get; set; } = String.Empty;

    public IList<WebsiteConfigModel> WebsiteConfig { get; set; } = new List<WebsiteConfigModel>();
}