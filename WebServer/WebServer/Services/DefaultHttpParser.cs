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
                return line.Trim().Substring(key.Length + 1);
            }
        }

        return string.Empty;
    }

    public HttpRequestModel ParseHttpRequest(string input)
    {
        var model = new HttpRequestModel();
        //Splitting all logged data into array lines
        string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        //Extracting logged data and placing them into HTTPRequestModel
        model.Host = ExtractValue(lines, "Host:");
        model.MethodType = lines[0]; //first line is request type: GET, PUT, POST, DELETE etc
        model.Connection = ExtractValue(lines, "Connection:");

        var userAgent = ExtractValue(lines, "User-Agent:");
        var secChUa = ExtractValue(lines, "sec-ch-ua:");
        var contentLength = ExtractValue(lines, "Content-Length:");
        var accecpt = ExtractValue(lines, "Accept:");
        var secFetchSite = ExtractValue(lines, "Sec-Fetch-Site:");
        var secFetchMode = ExtractValue(lines, "Sec-Fetch-Mode:");
        var secFetchDest = ExtractValue(lines, "Sec-Fetch-Dest:");
        var acceptEncoding = ExtractValue(lines, "Accept-Encoding:");
        var cookie = ExtractValue(lines, "Cookie:");

        model.Headers.Add(new KeyValuePair<string, string>("Host:", model.Host));
        model.Headers.Add(new KeyValuePair<string, string>("Request type:", model.MethodType));
        model.Headers.Add(new KeyValuePair<string, string>("Connection:", model.Connection));
        model.Headers.Add(new KeyValuePair<string, string>("User-Agent:", userAgent));
        model.Headers.Add(new KeyValuePair<string, string>("sec-ch-ua:", secChUa));
        model.Headers.Add(new KeyValuePair<string, string>("Content-Length:", contentLength));
        model.Headers.Add(new KeyValuePair<string, string>("Accept:", accecpt));
        model.Headers.Add(new KeyValuePair<string, string>("Sec-Fetch-Site:", secFetchSite));
        model.Headers.Add(new KeyValuePair<string, string>("Sec-Fetch-Mode:", secFetchMode));
        model.Headers.Add(new KeyValuePair<string, string>("Sec-Fetch-Dest:", secFetchDest));
        model.Headers.Add(new KeyValuePair<string, string>("Accept-Encoding:", acceptEncoding));
        model.Headers.Add(new KeyValuePair<string, string>("Cookie:", cookie));
        
       
        return model;
    }
    

    private static void Show(IList<KeyValuePair<string, string>> header)
    {
        Console.WriteLine("TESTING PRINTING::\n");

        // Iterate through list
        foreach (KeyValuePair<string, string> str in header)
        {
            // Print
            Console.WriteLine("\t" + str);
        }
    }
}