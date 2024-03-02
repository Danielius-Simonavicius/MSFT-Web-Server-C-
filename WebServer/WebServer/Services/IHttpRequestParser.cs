using WebServer.Models;

namespace WebServer.Services;

public interface IHttpRequestParser
{
    HttpRequestModel ParseHttpRequest(string input);
}