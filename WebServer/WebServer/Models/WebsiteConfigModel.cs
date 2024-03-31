namespace WebServer.Models;

public class WebsiteConfigModel
{
    public string AllowedHosts { get; set; } = string.Empty;
    public string DefaultPage { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
    public string Path { get; set; } = string.Empty;
    public int WebsitePort { get; set; }
}