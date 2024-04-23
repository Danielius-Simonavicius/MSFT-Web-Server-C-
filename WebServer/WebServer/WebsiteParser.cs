using System.IO.Compression;
using System.Text;

namespace WebServer.WebSites;

public class WebsiteParser
{
    public void ExtractWebsiteFile(byte[] zipData )
    {
        // byte[] receivedBytes = new byte[expectedBytes];
        // receivedBytes = Encoding.UTF8.GetBytes(data);

        //string[] sections = input.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries); //0 header
        // if (sections.Length < 3)
        // {
        //     Console.WriteLine("Error while extracting File");
        //     return;
        // }

        // Get the binary data of the zip file from section [2]
        //byte[] zipData = Encoding.UTF8.GetBytes(sections[2]);

        // Create a memory stream from the binary data
        using MemoryStream memoryStream = new MemoryStream(zipData);
        // Specify the directory where the zip file will be extracted
        const string extractDirectory = "/Users/danieljr/Desktop/Projects/MSFT-Web-Server-C-/WebServer/WebServer/WebSites";

        // Extract the zip file to the specified directory
        using (var archive = new ZipArchive(memoryStream,ZipArchiveMode.Read))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // Construct the full path for the entry
                string fullPath = Path.Combine(extractDirectory, entry.FullName);

                // Ensure that the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                // Extract the entry to the specified path
                entry.ExtractToFile(fullPath, true);
            }
        }

        Console.WriteLine("Zip file extracted successfully to directory: " + extractDirectory);
    }
}