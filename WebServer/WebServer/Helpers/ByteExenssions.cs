using System.Text;
using static System.Text.RegularExpressions.Regex;

namespace WebServer.Helpers;

public static class ByteExenssions
{
    public static string ExtractValue(this byte[] data, string key)
    {
        var input = Encoding.UTF8.GetString(data);
        var pattern = $@"\r\nContent-Disposition: form-data; name=""{key}""\r\n\r\n(.+?)\r\n";
        var _match = Match(input, pattern);

        return _match.Groups[1].Value;
    } 
}