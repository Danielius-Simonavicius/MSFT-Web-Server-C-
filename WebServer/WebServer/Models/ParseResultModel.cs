namespace WebServer.Models;

public class ParseResultModel
{
    public WebsiteConfigModel? NewWebsite { get; set; }
    public byte[]? FileContent { get; set; }
    public string UniqueFolderName { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"UniqueFolderName: {UniqueFolderName}, FileContent {FileContent?.Length ?? 0} bytes.";
    }
}