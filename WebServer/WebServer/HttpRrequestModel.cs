namespace WebServer;

public class HttpRequestModel
{
    public string Host { get; set; }
    private IList<KeyValuePair<string, string>> LoggedData { get; set; } = new List<KeyValuePair<string, string>>();
    //keyValuePairs.Add(new KeyValuePair<string, string>("Name", "John"));
    private static string ExtractValue(string[] lines, string key)
    {
        foreach (var line in lines)
        {
            if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(key.Length).Trim();
            }
        }

        return string.Empty;
    }

    public IList<KeyValuePair<string, string>> ParseHttpRequest(string input)
    {
        //HttpRequestModel httpRequest = new HttpRequestModel();

        // Split input by lines
        string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        
        // Extract RequestData and Host
        string Host = ExtractValue(lines, "Host:");
        Console.WriteLine($"EXTRACTED line: {Host}");
        LoggedData.Add(new KeyValuePair<string, string>("Host:", Host));
        
        //
        // // Extract headers after 'Host'
        // int startIndex = Array.IndexOf(lines, "Host:") + 1;
        // for (int i = startIndex; i < lines.Length; i++)
        // {
        //     string line = lines[i].Trim();
        //     if (!string.IsNullOrEmpty(line))
        //     {
        //         // Split line into key-value pair
        //         string[] headerParts = line.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
        //         if (headerParts.Length == 2)
        //         {
        //             string key = headerParts[0].Trim();
        //             string value = headerParts[1].Trim();
        //             httpRequest.RequestHeaders.Add(new KeyValuePair<string, string>(key, value));
        //         }
        //     }
        // }

        return LoggedData;
    }
}