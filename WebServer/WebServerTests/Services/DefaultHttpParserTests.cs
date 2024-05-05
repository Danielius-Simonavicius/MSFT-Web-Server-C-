using WebServer.Services;

namespace WebServerTests.Services;

public class DefaultHttpParserTests
{
    [Fact]
    public void ParseHttpRequest_ReturnsModel()
    {
        string httpInput = @"GET / HTTP/1.1
      Host: localhost:8080
      Connection: keep-alive
      User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36
      Accept: */*
      Sec-Fetch-Site: none
      Sec-Fetch-Mode: cors
      Sec-Fetch-Dest: empty
      Accept-Encoding: gzip, deflate, br
      Accept-Language: en-IE,en-US;q=0.9,en;q=0.8
      Cookie: .AspNetCore.Antiforgery.ZaPy40bDLtQ=CfDJ8CHPvO_zz11JjaMXNV9ruLMZWNMiWsgCHIeg0LlgIHi4ClDpW5nSkySlIs-9gKa0NYPO5Fvbfizx6iwloJupskWaQcqCw9vj-JY2zCyj7EVTWiH_G-mGUhS32QME1rB4tjjLOpKAwuYH_p9zrqa37Ec

";

        var service = new DefaultHttpParser();

        var model = service.ParseHttpRequest(httpInput);
        Assert.Equal("localhost:8080", model.Host);
        Assert.Equal("GET", model.RequestType);
        
        
        Assert.Contains(new KeyValuePair<string, string>("Connection", "keep-alive"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Accept", "*/*"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Sec-Fetch-Site", "none"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Sec-Fetch-Mode", "cors"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Sec-Fetch-Dest", "empty"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Accept-Encoding", "gzip, deflate, br"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Accept-Language", "en-IE,en-US;q=0.9,en;q=0.8"), model.Headers);
        Assert.Contains(new KeyValuePair<string, string>("Cookie", ".AspNetCore.Antiforgery.ZaPy40bDLtQ=CfDJ8CHPvO_zz11JjaMXNV9ruLMZWNMiWsgCHIeg0LlgIHi4ClDpW5nSkySlIs-9gKa0NYPO5Fvbfizx6iwloJupskWaQcqCw9vj-JY2zCyj7EVTWiH_G-mGUhS32QME1rB4tjjLOpKAwuYH_p9zrqa37Ec"), model.Headers);
        
    }
}