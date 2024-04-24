using System.IO.Compression;
using System.Text;

namespace WebServer.WebSites;

public class WebsiteParser
{
    public void ExtractWebsiteFile(byte[] zipData)
    {
        // Convert byte array to memory stream
        using (MemoryStream stream = new MemoryStream(zipData))
        {
            // Create ZIP archive from memory stream
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // Extract each entry in the ZIP archive
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(Path.GetFileName(entry.FullName)))
                        continue;

                    // Combine output path with entry's name
                    string filePath =
                        Path.Combine("/Users/danieljr/Desktop/Projects/MSFT-Web-Server-C-/WebServer/WebServer/WebSites",
                            entry.FullName);

                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    // Extract entry to file
                    entry.ExtractToFile(filePath, true);
                }
            }
        }
    }

    public byte[] ExtractBinaryData(byte[] data, byte[] contentType)
    {
        // Find the index of the content type in the data byte array
        int contentTypeIndex = IndexOfSequence(data, contentType);

        if (contentTypeIndex != -1)
        {
            // Calculate the start index of the binary data
            int startIndex = contentTypeIndex + contentType.Length;

            // Extract the binary data
            byte[] binaryData = new byte[data.Length - startIndex];
            Array.Copy(data, startIndex, binaryData, 0, binaryData.Length);
            return RemoveLastTwoBytes(binaryData);
        }
        else
        {
            // Content type not found
            return null;
        }
    }

    // Function to find the index of a sequence of bytes in another byte array
    public static int IndexOfSequence(byte[] source, byte[] pattern)
    {
        for (int i = 0; i < source.Length - pattern.Length + 1; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return i;
            }
        }

        return -1; // Not found
    }
    
    public static byte[] RemoveLastTwoBytes(byte[] array)
    {
        if (array.Length >= 2)
        {
            // Create a new array with a length two bytes shorter than the original array
            byte[] result = new byte[array.Length - 2];

            // Copy the bytes from the original array to the new array, excluding the last two bytes
            Array.Copy(array, 0, result, 0, array.Length - 2);

            return result;
        }
        else
        {
            // If the original array doesn't have enough bytes, return an empty array or handle the error as needed
            return new byte[0];
        }
    }

}