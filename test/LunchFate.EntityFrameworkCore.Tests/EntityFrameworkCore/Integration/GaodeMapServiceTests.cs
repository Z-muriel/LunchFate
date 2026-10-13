using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LunchFate.Integration.Gaode;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Integration;

/// <summary>
/// 用固定响应的桩 HttpClient 验证周边搜索对高德 status 字段的判定（不发真实请求）：
/// status=0（限流、Key 无效）必须标记 Failed 并留下日志素材；
/// status=1 且 pois 为空是"真没结果"，不得误判为失败——否则前端会把无结果报成"搜索失败"。
/// </summary>
public class GaodeMapServiceTests
{
    private const string RateLimitedResponse =
        """{"status":"0","info":"CUQPS_HAS_EXCEEDED_THE_LIMIT","infocode":"10021"}""";

    private const string EmptyResultResponse =
        """{"status":"1","info":"OK","infocode":"10000","count":"0","pois":[]}""";

    /// <summary>status=0 时高德不返回 pois 字段，必须按失败返回，而不是当成空结果页。</summary>
    [Fact]
    public async Task Should_Mark_Failed_When_Status_Is_Not_Success()
    {
        var service = CreateService(RateLimitedResponse);

        var page = await service.SearchAroundAsync("面馆", "050000", 113.293333, 23.092112, 3000);

        page.Failed.ShouldBeTrue();
        page.Items.ShouldBeEmpty();
    }

    /// <summary>status=1 且 pois 为空数组表示附近确实没有匹配，按成功空页返回。</summary>
    [Fact]
    public async Task Should_Return_Empty_Page_When_Status_Is_Success_But_No_Pois()
    {
        var service = CreateService(EmptyResultResponse);

        var page = await service.SearchAroundAsync("面馆", "050000", 113.293333, 23.092112, 3000);

        page.Failed.ShouldBeFalse();
        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
    }

    /// <summary>用桩 handler 构造只回固定 JSON 的高德服务，避免测试依赖网络与真实 Key。</summary>
    private static GaodeMapService CreateService(string responseJson)
    {
        var handler = new StubHttpMessageHandler(responseJson);
        var client = new HttpClient(handler)
        {
            BaseAddress = new System.Uri("https://restapi.amap.com")
        };

        return new GaodeMapService(
            client,
            Options.Create(new GaodeMapOptions { Key = "测试Key", Enabled = true }),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<GaodeMapService>.Instance);
    }

    /// <summary>总是返回同一段 JSON 的最简 HTTP 桩。</summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public StubHttpMessageHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
