using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
        //test 
        var model = new HttpRequestModel();
        //Splitting all logged data into array lines
        string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        //Extracting logged data and placing them into HTTPRequestModel
        model.Host = ExtractValue(lines, "Host");
        
        string[] lineOneParts = lines[0].Split(" "); //splitting 1st line into parts EG. "GET[0], /path[1], HTTP1.1[2]" 
        model.RequestType = lineOneParts[0]; //Request type GET PUT POST DELETE 
        model.Path = lineOneParts[1];// /path/to/file
        model.Connection = ExtractValue(lines, "Connection");


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

        return model;
    }
}