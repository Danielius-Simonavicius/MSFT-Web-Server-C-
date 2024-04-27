namespace WebServer.Models;

public class ParseResultModel
{
    public WebsiteConfigModel? NewWebsite { get; set; }
    public byte[]? FileContent { get; set; }
    public string UniqueFolderName { get; set; } = string.Empty;
}