using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebServer.Helpers;
using WebServer.Models;
using static System.Text.RegularExpressions.Regex;

namespace WebServer.Services;

public class WebsiteHostingService : IWebsiteHostingService
{
    private readonly ILogger<WebsiteHostingService> _logger;
    private readonly IConfigurationService _configurationService;

    private readonly IMessengerService _messengerService;

    public WebsiteHostingService(ILogger<WebsiteHostingService> logger,
        IMessengerService messengerService,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _messengerService = messengerService;
        _configurationService = configurationService;
    }

    public void LoadWebsite(byte[] data, HttpRequestModel request)
    {
        _logger.LogInformation($"LoadWebsite: {data.Length} bytes");
        // Split the byte array by ------Webkitboundary
        var parsedResult = ParseUploadData(data, request.ContentType);
        _logger.LogInformation($"LoadWebsite AFTER ParseUploadData: {parsedResult}");
        //If extracting file wasn't successful, dont add to WebsiteConfig.json
        if (!ExtractAndUnzipWebsiteFile(parsedResult.FileContent,
                parsedResult.UniqueFolderName)) return;
        try
        {
            var serverConfig = this._configurationService.GetSettings();
            // Access the "Websites" array within the ServerConfig
            var websites = serverConfig.Websites;
            // Add the new website configuration to the array
            if (parsedResult.NewWebsite == null) return;
            websites.Add(parsedResult.NewWebsite);
            if (_configurationService.SaveConfig(serverConfig))
            {
                _messengerService.SendNewWebsiteEvent(parsedResult.NewWebsite);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, null);
            throw;
        }
    }

    public ParseResultModel? ParseUploadData(byte[] data, string contentType)
    {
        try
        {
            _logger.LogInformation($"ParseUploadData: {data.Length} bytes, {contentType}");
            ParseResultModel result = new ParseResultModel();
            var match = Match(contentType, @"boundary=(?<boundary>.+)");
            var boundary = match.Success ? match.Groups["boundary"].Value.Trim() : "";

            var delimiter = Encoding.UTF8.GetBytes($"--{boundary}" );

            //Splitting upload data into parts by the boundary (0 is header, 1 in file content, 2 and onwards is part of form data object)
            var parts = SplitByteArray(data, delimiter);
            parts.ToList()
                .ForEach(part =>
                {
                    _logger.LogInformation($"ParseUploadData: {parts.IndexOf(part)} -> size {part.Length}"); 
                });
            
            //This removes the file-contents header
            result.FileContent = ExtractFileContent(parts);
            result.UniqueFolderName = Guid.NewGuid().ToString();
            _logger.LogInformation($"ParseUploadData finishing: {result}");
            result.NewWebsite = new WebsiteConfigModel
            {
                WebsiteId = result.UniqueFolderName,
                WebsiteName = data.ExtractValue("WebsiteName"),
                AllowedHosts = data.ExtractValue("AllowedHosts"),
                Path = $"{result.UniqueFolderName}/{data.ExtractValue("Path")}",
                DefaultPage = data.ExtractValue("DefaultPage"),
                WebsitePort = FindAvailablePort()
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, null);
            throw;
        }
       
    }
    
   

    private bool ExtractAndUnzipWebsiteFile(byte[] zipData, string uniqueFolderName)
    {
        _logger.LogInformation($"ExtractAndUnzipWebsiteFile: {zipData.Length} bytes");
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

                var config = _configurationService.GetSettings();
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
            _logger.LogInformation(ex.ToString());
            return false; //failed to extract file
        }

        return true;
    }

    private byte[] ExtractFileContent(IList<byte[]> byteArrays)
    {
        try
        {
            const string pkSignature = "PK";
            var pkBytes = Encoding.UTF8.GetBytes(pkSignature);
            foreach (var byteArray in byteArrays)
            {
                var index = IndexOf(byteArray, pkBytes, 0);
                if (index != -1)
                {
                    // Return the file content byte array starting from "PK"
                    return SubArray(byteArray, index, byteArray.Length - index);
                }
            }
          
        }
        catch (Exception e)
        {
            _logger.LogInformation("Failed at extracting file content");
        }

        return null; // or throw an exception
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