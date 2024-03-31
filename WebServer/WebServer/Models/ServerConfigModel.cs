namespace WebServer.Models;

public class ServerConfigModel
{
    public string RootFolder { get; set; } = String.Empty;

    public IList<WebsiteConfigModel> Websites { get; set; } = new List<WebsiteConfigModel>();
}