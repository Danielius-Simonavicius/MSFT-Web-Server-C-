namespace WebServer.Models;

public class WebsiteListModel
{
    public IList<WebsiteConfigModel> WebsiteConfigList { get; set; } = new List<WebsiteConfigModel>();
}