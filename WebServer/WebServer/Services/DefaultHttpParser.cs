using System.Runtime.InteropServices;
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
                return line.Trim().Substring(key.Length+1);
            }
        }

        return string.Empty;
    }
    
    public HttpRequestModel ParseHttpRequest(string input)
    {
        var model = new HttpRequestModel();
        string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        
        
        model.Host = ExtractValue(lines, "Host:");
        model.MethodType = lines[0];
        //ExtractValue(lines, "Request Data:").Split(' ')[1];
       
        // model.Headers.Add(new KeyValuePair<string, string>("Host:", Host));
        // model.Headers.Add(new KeyValuePair<string, string>("Method:", MethodType));
        // model.Headers.Add(new KeyValuePair<string, string>("Connection:", Connection));
        // model.Headers.Add(new KeyValuePair<string, string>("User-Agent:", UserAgent));

        //TODO: parse all other values into Header list.
        return model;
    }
}