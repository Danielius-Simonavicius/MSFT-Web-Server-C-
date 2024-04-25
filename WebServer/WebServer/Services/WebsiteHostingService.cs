using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using WebServer.Models;
using static System.Text.RegularExpressions.Regex;

namespace WebServer.Services;

public class WebsiteHostingService
{
    public static void LoadWebsite(byte[] data, HttpRequestModel request, ServerConfigModel config)
    {
        // Split the byte array by ------Webkitboundary
        ParseUploadData(data, request.ContentType, out var newWebsiteModel, out var fileContent);

        //If extracting file was successful start new connection thread
        if (ExtractAndUnzipWebsiteFile(fileContent, config))
        {
            var test = newWebsiteModel;
            //TODO add to start server method with new website

            // var list = JsonConvert.DeserializeObject<List<WebsiteConfigModel>>(WebsiteConfigModel);
            // list.Add(new WebsiteConfigModel();
            //var convertedJson = JsonConvert.SerializeObject(list, Formatting.Indented);


            // _thread = new Thread(() => ConnectionThreadMethod(website, stoppingToken));
            // _thread.Start();
        }
    }

    public static void ParseUploadData(byte[] data, string contentType, out WebsiteConfigModel newWebsite,
        out byte[] fileContent)
    {
        var match = Match(contentType,
            @"boundary=(?<boundary>.+)");
        var boundary = match.Success ? match.Groups["boundary"].Value.Trim() : "";

        var delimiter = Encoding.ASCII.GetBytes("--" + boundary);

        //Splitting upload data into parts by the boundry (0 is header, 1 in file content, 2 and onwards is part of form data object)
        var parts = SplitByteArray(data, delimiter);
        var stringParts = parts.ConvertAll(bytes => Encoding.ASCII.GetString(bytes)).ToArray();

        //This removes the filecontents header
        fileContent = ExtractFileContent(parts[1]);

        newWebsite = new WebsiteConfigModel
        {
            AllowedHosts = ExtractValue("AllowedHosts"),
            Path = ExtractValue("Path"),
            DefaultPage = ExtractValue("DefaultPage"),
            WebsitePort = FindAvailablePort()
        };

        string ExtractValue(string key)
        {
            var input = Encoding.ASCII.GetString(data);
            var pattern = $@"\r\nContent-Disposition: form-data; name=""{key}""\r\n\r\n(.+?)\r\n";
            var _match = Match(input, pattern);

            return _match.Groups[1].Value;
        }
    }

    private static bool ExtractAndUnzipWebsiteFile(byte[] zipData, ServerConfigModel config)
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
                var filePath =
                    Path.Combine(config.RootFolder,
                        entry.FullName);

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

        var model = new WebsiteConfigModel();
        return true;
    }

    private static byte[] ExtractFileContent(byte[] byteArray)
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

    private static List<byte[]> SplitByteArray(byte[] byteArray, byte[] delimiter)
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

    private static int IndexOf(byte[] array, byte[] pattern, int startIndex)
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

    private static byte[] SubArray(byte[] array, int startIndex, int length)
    {
        var result = new byte[length];
        Array.Copy(array, startIndex, result, 0, length);
        return result;
    }

    public static int FindAvailablePort(int startingPort = 8000, int maxPort = 65535)
    {
        for (var port = startingPort; port <= maxPort; port++)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
        }

        throw new System.Exception("No available ports found in the specified range.");
    }

    private static bool IsPortAvailable(int port)
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