using WebServer.Models;

namespace WebServer.Services;

public interface IConfigurationService
{
    ServerConfigModel GetSettings();
    bool SaveConfig(ServerConfigModel model);
    public void RemoveWebsiteFromConfig(string websiteId,out WebsiteConfigModel? websiteRemoved);

    public void EditWebsiteInConfig(string websiteId, WebsiteConfigModel editedWebsite);
}