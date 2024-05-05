using WebServer.Models;

namespace WebServer.Services;

public interface IWebsiteHostingService
{
    void LoadWebsite(byte[] data, HttpRequestModel request);

    ParseResultModel? ParseUploadData(byte[] data, string contentType);
    

}