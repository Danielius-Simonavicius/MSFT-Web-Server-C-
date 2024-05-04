using Newtonsoft.Json;
using WebServer.Models;

namespace WebServer.Services;

public class DefaultConfigurationService : IConfigurationService
{
    private readonly string jsonFilePath = "./WebsiteConfig.json";
    private readonly ILogger _logger;
    private IMessengerService _messengerService;
    public DefaultConfigurationService(ILogger<DefaultConfigurationService> logger,
        IMessengerService messengerService)
    {
        _logger = logger;
        _messengerService = messengerService;
    }

    public ServerConfigModel GetSettings()
    {
        _logger.LogInformation("Reloading Website configuration");
        // Read JSON file contents into a string
        var jsonContent = File.ReadAllText(jsonFilePath);
        var serverConfig = JsonConvert.DeserializeObject<ServerConfigModel>(jsonContent);
        return serverConfig!;
    }

    public bool SaveConfig(ServerConfigModel model)
    {
        // Serialize the updated ServerConfig object back to JSON
        var updatedConfig = JsonConvert.SerializeObject(model, Formatting.Indented);

        // Write the updated JSON back to the file
        File.WriteAllText(jsonFilePath, updatedConfig);
        return true;
    }

    public void RemoveWebsiteFromConfig(string websiteId, out WebsiteConfigModel? websiteRemoved)
    {
        // Read JSON file contents into a string
        var serverConfig = GetSettings();

        var index = -1;
        for (var i = 0; i < serverConfig!.Websites.Count; i++)
        {
            if (serverConfig.Websites[i].WebsiteId == websiteId)
            {
                index = i;
                break;
            }
        }

        if (index != -1)
        {
            websiteRemoved = serverConfig.Websites[index];
            serverConfig.Websites.RemoveAt(index);

            var updatedConfig = JsonConvert.SerializeObject(serverConfig, Formatting.Indented);

            // Write the updated JSON back to the file
            File.WriteAllText(jsonFilePath, updatedConfig);
        }
        else
        {
            _logger.LogInformation("Website {websiteId} not found", websiteId);
            websiteRemoved = null;
        }
    }

    public void EditWebsiteInConfig(string websiteId, WebsiteConfigModel editedWebsite)
    {
        var serverConfig = GetSettings();
        var webSiteInList = serverConfig.Websites.FirstOrDefault(x => x.WebsiteId == websiteId);
        if (webSiteInList == null)
        {
            _logger.LogInformation("Website {websiteId} not found", websiteId);
            return;
        }

        //removing website from config
        serverConfig.Websites.Remove(webSiteInList);
        //adding edited version to config
        serverConfig.Websites?.Add(editedWebsite);
        //serializing updated config
        var updatedConfig = JsonConvert.SerializeObject(serverConfig, Formatting.Indented);
        // Write the updated JSON back to the file
        File.WriteAllText(jsonFilePath, updatedConfig);
        _messengerService.SendConfigChangedEvent();
    }
}