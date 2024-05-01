using WebServer.Models;

namespace WebServer.Services;

public interface IGetResponseService
{
    byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website, ServerConfigModel config);
}