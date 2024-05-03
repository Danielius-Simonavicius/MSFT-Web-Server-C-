using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebServer.Models;
using static System.Text.RegularExpressions.Regex;

namespace WebServer.Services;

public class WebsiteHostingService : IWebsiteHostingService
{
    private readonly ILogger<WebsiteHostingService> _logger;
    private readonly string jsonFilePath = "./WebsiteConfig.json";

    private readonly IMessengerService _messengerService;

    public WebsiteHostingService(ILogger<WebsiteHostingService> logger,
        IMessengerService messengerService)
    {
        _logger = logger;
        _messengerService = messengerService;
    }

    public void LoadWebsite(byte[] data, HttpRequestModel request, ServerConfigModel config)
    {
        // Split the byte array by ------Webkitboundary
        var parsedResult = ParseUploadData(data, request.ContentType);

        //If extracting file wasn't successful, dont add to WebsiteConfig.json
        if (!ExtractAndUnzipWebsiteFile(parsedResult.FileContent,
                config, parsedResult.UniqueFolderName)) return;
        try
        {
            var serverConfig = GetSettings();
            // Access the "Websites" array within the ServerConfig
            var websites = serverConfig.Websites;

            // Add the new website configuration to the array
            websites?.Add(parsedResult.NewWebsite);

            // Serialize the updated ServerConfig object back to JSON
            var updatedConfig = JsonConvert.SerializeObject(serverConfig, Formatting.Indented);

            // Write the updated JSON back to the file
            File.WriteAllText(jsonFilePath, updatedConfig);

            _messengerService.SendNewWebsiteEvent(parsedResult.NewWebsite);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, null);
            throw;
        }
    }

    public ParseResultModel? ParseUploadData(byte[] data, string contentType)
    {
        ParseResultModel result = new ParseResultModel();
        var match = Match(contentType,
            @"boundary=(?<boundary>.+)");
        var boundary = match.Success ? match.Groups["boundary"].Value.Trim() : "";

        var delimiter = Encoding.ASCII.GetBytes("--" + boundary);

        //Splitting upload data into parts by the boundry (0 is header, 1 in file content, 2 and onwards is part of form data object)
        var parts = SplitByteArray(data, delimiter);
        var stringParts = parts.ConvertAll(bytes => Encoding.ASCII.GetString(bytes)).ToArray();

        //This removes the filecontents header
        result.FileContent = ExtractFileContent(parts[1]);
        result.UniqueFolderName = Guid.NewGuid().ToString();
        result.NewWebsite = new WebsiteConfigModel
        {
            WebsiteId = result.UniqueFolderName,
            WebsiteName = ExtractValue("WebsiteName"),
            AllowedHosts = ExtractValue("AllowedHosts"),
            Path = $"{result.UniqueFolderName}/{ExtractValue("Path")}",
            DefaultPage = ExtractValue("DefaultPage"),
            WebsitePort = FindAvailablePort()
        };

        return result;

        string ExtractValue(string key)
        {
            var input = Encoding.ASCII.GetString(data);
            var pattern = $@"\r\nContent-Disposition: form-data; name=""{key}""\r\n\r\n(.+?)\r\n";
            var _match = Match(input, pattern);

            return _match.Groups[1].Value;
        }
    }

    public ServerConfigModel GetSettings()
    {
        // Read JSON file contents into a string
        var jsonContent = File.ReadAllText(jsonFilePath);
        var serverConfig = JsonConvert.DeserializeObject<ServerConfigModel>(jsonContent);
        return serverConfig!;
    }

    public void EditWebsiteInConfig(string websiteId, WebsiteConfigModel editedWebsite)
    {
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
            //removing website from config
            serverConfig.Websites.RemoveAt(index);
            
            //adding edited version to config
            serverConfig.Websites?.Add(editedWebsite);

            //serializing updated config
            var updatedConfig = JsonConvert.SerializeObject(serverConfig, Formatting.Indented);

            // Write the updated JSON back to the file
            File.WriteAllText(jsonFilePath, updatedConfig);
        }
        else
        {
            _logger.LogCritical("Website ID not found");
        }

    }

    public void RemoveWebsiteFromConfig(string websiteId, out WebsiteConfigModel websiteRemoved)
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
            _logger.LogCritical("Website ID not found");
            websiteRemoved = null;
        }
    }

    private bool ExtractAndUnzipWebsiteFile(byte[] zipData, ServerConfigModel config, string uniqueFolderName)
    {
        try
        {
            // Convert byte array to memory stream
            using var stream = new MemoryStream(zipData);
            // Create ZIP archive from memory stream
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            // Extract each entry in the ZIP archive
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (string.IsNullOrEmpty(Path.GetFileName(entry.FullName)))
                    continue;

                // Combine output path with entry's name
                var filePath = Path.Combine(config.RootFolder, uniqueFolderName, entry.FullName);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                // Extract entry to file
                entry.ExtractToFile(filePath, true);
            }
        }
        catch (Exception ex)
        {
            return false; //failed to extract file
        }

        return true;
    }

    private byte[] ExtractFileContent(byte[] byteArray)
    {
        const string pkSignature = "PK";
        var pkBytes = Encoding.ASCII.GetBytes(pkSignature);
        var index = IndexOf(byteArray, pkBytes, 0);
        if (index != -1)
        {
            // Return the file content byte array starting from "PK"
            return SubArray(byteArray, index, byteArray.Length - index);
        }

        // Handle case when "PK" is not found
        return null!; // or throw an exception
    }

    private List<byte[]> SplitByteArray(byte[] byteArray, byte[] delimiter)
    {
        var parts = new List<byte[]>();
        var delimiterIndex = IndexOf(byteArray, delimiter, 0);

        while (delimiterIndex != -1)
        {
            parts.Add(SubArray(byteArray, 0, delimiterIndex));
            byteArray = SubArray(byteArray, delimiterIndex + delimiter.Length,
                byteArray.Length - delimiterIndex - delimiter.Length);
            delimiterIndex = IndexOf(byteArray, delimiter, 0);
        }

        if (byteArray.Length > 0)
        {
            parts.Add(byteArray);
        }

        return parts;
    }

    private int IndexOf(byte[] array, byte[] pattern, int startIndex)
    {
        for (var i = startIndex; i <= array.Length - pattern.Length; i++)
        {
            int j;
            for (j = 0; j < pattern.Length; j++)
            {
                if (array[i + j] != pattern[j])
                {
                    break;
                }
            }

            if (j == pattern.Length)
            {
                return i;
            }
        }

        return -1;
    }

    private byte[] SubArray(byte[] array, int startIndex, int length)
    {
        var result = new byte[length];
        Array.Copy(array, startIndex, result, 0, length);
        return result;
    }

    private int FindAvailablePort(int startingPort = 8000, int maxPort = 65535)
    {
        for (var port = startingPort; port <= maxPort; port++)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
        }

        throw new Exception("No available ports found in the specified range.");
    }

    private bool IsPortAvailable(int port)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}