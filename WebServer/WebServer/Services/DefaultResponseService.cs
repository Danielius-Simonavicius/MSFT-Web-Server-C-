using System.Text;
using Newtonsoft.Json;
using WebServer.Models;

namespace WebServer.Services;

public class DefaultResponseService : IGetResponseService
{
    private readonly IWebsiteHostingService _websiteHostingService;
    private readonly IMessengerService _messengerService;
    private ServerConfigModel? _websitesConfigurations;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger _logger;
    public DefaultResponseService(IWebsiteHostingService websiteHostingService, 
        IMessengerService messengerService,
        IConfigurationService configurationService,
        ILogger<DefaultResponseService> logger)
    {
        _websiteHostingService = websiteHostingService;
        _messengerService = messengerService;
        _configurationService = configurationService;
        _logger = logger;
    }

    private bool LoadConfig()
    {
        this._websitesConfigurations = _configurationService.GetSettings();
        return _websitesConfigurations != null;
    }
    
 
    
    
    
    public byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website, ServerConfigModel config)
    {
        const string statusCode = "200 OK";
        string fileName = NormalizeFileName(requestModel.Path, website.DefaultPage);
        string methodType = requestModel.RequestType;
        string requestedFile = Path.Combine(config.RootFolder, website.Path, fileName);
        
        
        switch (methodType)
        {
            case "GET" when fileName.Equals("api/admin/sites"):
                return HandleGetRequest(fileName, website, statusCode);
            case "POST":
                return HandlePostRequest(fileName, website, statusCode);
            case "PUT":
                return HandlePutRequest(fileName, requestModel, website, statusCode);
            case "DELETE":
                return HandleDeleteRequest(fileName, website, statusCode);
            case "OPTIONS":
                return OptionsResponse(website);
        }
        
        if (!File.Exists(requestedFile))
        {
            _logger.LogInformation("File not found: {requestedFile}", requestedFile);
            return NotFound404(website);
        }

        return ServeFile(requestedFile, website, statusCode);
    }

    private string NormalizeFileName(string fileName, string defaultPage)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Equals("/"))
            return defaultPage;

        return fileName.TrimStart('/');
    }

    private byte[] HandleGetRequest(string fileName, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.Equals("api/admin/sites"))
        {
            this.LoadConfig();
            var responseHeader = BuildHeader(statusCode, "application/json; charset=UTF-8", website);
            if (_websitesConfigurations == null)
            {
                if (!this.LoadConfig())
                {
                    return [];
                }
            }
            var websites = _websitesConfigurations!.Websites;

            var websiteBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(websites));
            return Encoding.ASCII.GetBytes(responseHeader).Concat(websiteBytes).ToArray();
        }

        return [];
    }

    private byte[] HandlePostRequest(string fileName, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.Equals("api/uploadWebsite"))
        {
            var responseHeader = BuildHeader(statusCode, null, website);
            return Encoding.ASCII.GetBytes(responseHeader).ToArray();
        }

        return Array.Empty<byte>();
    }

    private byte[] ServeFile(string filePath, WebsiteConfigModel website, string statusCode)
    {
        var file = File.ReadAllBytes(filePath);
        var contentType = FindContentType(filePath);
        var responseHeader = BuildHeader(statusCode, $"{contentType}; charset=UTF-8", website);
        return Encoding.ASCII.GetBytes(responseHeader).Concat(file).ToArray();
    }

    private string BuildHeader(string statusCode, string contentType, WebsiteConfigModel website)
    {
        var builder = new StringBuilder($"HTTP/1.1 {statusCode}\r\n")
            .Append("Server: Microsoft_web_server\r\n");

        if (contentType != null)
        {
            builder.Append($"Content-Type: {contentType}\r\n");
        }

        builder.Append($"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n");
        return builder.ToString();
    }

    private byte[] HandleDeleteRequest(string fileName, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.StartsWith("api/admin/site"))
        {
            var responseHeader = BuildHeader(statusCode, "application/json; charset=UTF-8", website);
            var WebsiteId = fileName.Substring(fileName.LastIndexOf('/') + 1);
            //removing website from config
            _configurationService.RemoveWebsiteFromConfig(WebsiteId, out var websiteRemoved);
            //sending message to listeners which website was removed
            _messengerService.WebSiteRemovedEvent(websiteRemoved);
            this.LoadConfig();
            return Encoding.ASCII.GetBytes(responseHeader).ToArray();
        }

        return [];
    }
    
    private byte[] HandlePutRequest(string fileName, HttpRequestModel requestModel, WebsiteConfigModel website, string statusCode)
    {
        if (fileName.StartsWith("api/admin/site"))
        {
            var responseHeader = BuildHeader(statusCode, "application/json; charset=UTF-8", website);
            var WebsiteId = fileName.Substring(fileName.LastIndexOf('/') + 1);
            WebsiteConfigModel updatedWebsite = (WebsiteConfigModel) requestModel.BodyContent!;
            _configurationService.EditWebsiteInConfig(WebsiteId, updatedWebsite);
            this.LoadConfig();
            //sending message to listeners which website was edited
            return Encoding.ASCII.GetBytes(responseHeader).ToArray();
        }

        return [];
    }

    private byte[] OptionsResponse(WebsiteConfigModel website)
    {
        string statusCode = "200 OK";
        string responseHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            "Allow: GET, POST, OPTIONS, PUT, DELETE\r\n" +
            "Access-Control-Allow-Methods: GET, POST, OPTIONS, PUT, DELETE\r\n" +
            "Access-Control-Allow-Headers: Content-Type\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n" +
            "\r\n";

        var responseData = Encoding.ASCII.GetBytes(responseHeader);
        return responseData;
    }

    public static string FindContentType(string requestedFile)
    {
        switch (Path.GetExtension(requestedFile).ToLowerInvariant())
        {
            case ".js":
                return "text/javascript";
            case ".css":
                return "text/css";
            default:
                return "text/html";
        }
    }

    public static byte[] NotFound404(WebsiteConfigModel website)
    {
        var statusCode = "404 Not found";
        String responseHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";

        var responseData = Encoding.ASCII.GetBytes(responseHeader);
        return responseData.ToArray();
    }
}