using WebServer.Services;

namespace WebServerTests.Services;

public class DefaultHttpParserTests
{
    [Fact]
    public void ParseHttpRequest_ReturnsModel()
    {
        string httpInput = @"GET /favicon.ico HTTP/1.1
      Host: localhost:8080
      Connection: keep-alive
      Pragma: no-cache
      Cache-Control: no-cache
      sec-ch-ua: ""Not A(Brand"";v=""99"", ""Google Chrome"";v=""121"", ""Chromium"";v=""121""
      sec-ch-ua-mobile: ?0
      User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36
      sec-ch-ua-platform: ""macOS""
      Accept: image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8
      Sec-Fetch-Site: same-origin
      Sec-Fetch-Mode: no-cors
      Sec-Fetch-Dest: image
      Referer: http://localhost:8080/
      Accept-Encoding: gzip, deflate, br
      Accept-Language: en-GB,en-US;q=0.9,en;q=0.8
";

        var service = new DefaultHttpParser();

        var model = service.ParseHttpRequest(httpInput);
        Assert.Equal("localhost:8080", model.Host);
        Assert.Equal("GET /favicon.ico HTTP/1.1", model.MethodType);
        

    }
}