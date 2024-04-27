namespace WebServer.Models;

public class WebsiteConfigModel
{
    public string AllowedHosts { get; set; } = string.Empty;
    public string DefaultPage { get; set; } = "index.html";
    public string Path { get; set; } = string.Empty;
    public int WebsitePort { get; set; }
}