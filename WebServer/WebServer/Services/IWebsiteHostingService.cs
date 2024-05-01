using WebServer.Models;

namespace WebServer.Services;

public interface IWebsiteHostingService
{
    void LoadWebsite(byte[] data, HttpRequestModel request, ServerConfigModel config);

    ParseResultModel? ParseUploadData(byte[] data, string contentType);
    ServerConfigModel GetSettings();
    public void RemoveWebsiteFromConfig(string websiteToRemoveId,out WebsiteConfigModel websiteRemoved);
}