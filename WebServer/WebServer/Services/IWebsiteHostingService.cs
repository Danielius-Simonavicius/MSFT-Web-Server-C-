using WebServer.Models;

namespace WebServer.Services;

public interface IWebsiteHostingService
{
    void LoadWebsite(byte[] data, HttpRequestModel request, ServerConfigModel config);

    ParseResultModel? ParseUploadData(byte[] data, string contentType);
    ServerConfigModel GetSettings();
    public void RemoveWebsiteFromConfig(string websiteId,out WebsiteConfigModel websiteRemoved);

    public void EditWebsiteInConfig(string websiteId, WebsiteConfigModel editedWebsite);
}