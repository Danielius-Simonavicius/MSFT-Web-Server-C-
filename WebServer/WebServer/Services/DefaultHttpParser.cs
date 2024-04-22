using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WebServer.Models;
using WebServer.Models;

namespace WebServer.Services;

public class DefaultHttpParser : IHttpRequestParser
{
    private static string ExtractValue(string[] lines, string key)
    {
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim().Substring(key.Length + 1).Trim();
            }
        }

        return string.Empty;
    }

    public HttpRequestModel ParseHttpRequest(string input)
    {
        string[] sections =
            input.Split(new string[] { "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries); //0 header, 1 and so on is body values
        var model = new HttpRequestModel();
        //Splitting all logged data into array lines
        string[] lines = sections[0].Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        //Extracting logged data and placing them into HTTPRequestModel
        model.Host = ExtractValue(lines, "Host");

        string[] lineOneParts = lines[0].Split(" "); //splitting 1st line into parts EG. "GET[0], /path[1], HTTP1.1[2]" 
        model.RequestType = lineOneParts[0]; //Request type GET PUT POST DELETE


        model.RequestedPort = int.TryParse(model.Host.Split(':').LastOrDefault(), out int port) ? port : 0;

        model.Path = lineOneParts[1]; // /path/to/file
        model.Connection = ExtractValue(lines, "Connection");
        model.ContentType = ExtractValue(lines, "Content-Type");

        foreach (var line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2)
            {
                model.Headers.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
            }
            //some headers have more : so i just added the first part[0] as the key and the rest as the values.
            else if (parts.Length > 2)
            {
                string key = parts[0].Trim();
                string value = string.Join(":", parts.Skip(1)).Trim();
                model.Headers.Add(new KeyValuePair<string, string>(key, value));
            }
        }


        //Logic for extracting file content ie website folder
        if (sections.Length > 1)
        {
            // Split the body section into lines
            string[] bodyLines = sections[1].Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Concat(sections[2].Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();

            // string filenamePattern = @"filename\s*=\s*""([^""]*)""";
            // Regex regex = new Regex(filenamePattern);
            //
            // //Grabbing Filename
            // foreach (string line in bodyLines)
            // {
            //     if (regex.IsMatch(line))
            //     {
            //         Match match = regex.Match(line);
            //         model.FileInfo.FileName = match.Groups[1].Value;
            //     }
            // }

            // Extract the zip file content to a memory stream
            using (var memoryStream = new MemoryStream())
            {
                // Write zip file content to memory stream
                bool zipContentStarted = false;
                foreach (string line in bodyLines)
                {
                    if (zipContentStarted)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
                        memoryStream.Write(bytes, 0, bytes.Length);
                    }
                    else if (line.StartsWith("------WebKitFormBoundary"))
                    {
                        zipContentStarted = true;
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                
                // bool isValidZip = IsValidZipFile(memoryStream);
                //
                try
                {
                    // Extract zip file content to destination directory
                    using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zipArchive.Entries)
                        {
                            string fullPath =
                                Path.Combine(
                                    "/Users/danieljr/Desktop/Projects/MSFT-Web-Server-C-/WebServer/WebServer/WebSites",
                                    entry.FullName);
                            if (Path.GetFileName(fullPath) != string.Empty)
                            {
                                entry.ExtractToFile(fullPath, true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception
                    Console.WriteLine("Error extracting zip file: " + ex.Message);
                }
            }
        }

        return model;
    }
    
    public bool IsValidZipFile(Stream fileStream)
    {
        try
        {
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                // If reading the zip archive succeeds, the file is valid
                return true;
            }
        }
        catch (InvalidDataException)
        {
            // Catch InvalidDataException if the zip file is invalid or corrupted
            return false;
        }
    }
    
    // string zipPath = @"/Users/danieljr/Desktop/Projects/Assingment5CS230.zip";
    // string extractPath = @"/Users/danieljr/RiderProjects/TESTING/TESTING/FILE";
    //     
    // System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
}