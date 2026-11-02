using System.Net;
using System.Text;
using FluentAssertions;
using Umbraco.Commerce.ShippingProviders.Dhl.Api;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Dhl.UnitTests
{
    public class DhlExpressClientTests
    {
        [Fact]
        public async Task GetRatesAsync_Should_Post_To_The_Test_Endpoint_In_Test_Mode()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest());

            // asserts
            HttpRequestMessage request = handler.Requests.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri.Should().Be(new Uri("https://express.api.dhl.com/mydhlapi/test/rates"));
        }

        [Fact]
        public async Task GetRatesAsync_Should_Post_To_The_Live_Endpoint_Outside_Test_Mode()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), LiveSettings());

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest());

            // asserts
            HttpRequestMessage request = handler.Requests.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri.Should().Be(new Uri("https://express.api.dhl.com/mydhlapi/rates"));
        }

        [Fact]
        public async Task GetRatesAsync_Should_Authenticate_With_The_Test_Credentials_In_Test_Mode()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressSettings settings = TestSettings();
            settings.LiveUsername = "live-user";
            settings.LivePassword = "live-pass";
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), settings);

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest());

            // asserts
            HttpRequestMessage request = handler.Requests.Single();
            request.Headers.Authorization!.Scheme.Should().Be("Basic");
            DecodeBasic(request).Should().Be("test-user:test-pass");
        }

        [Fact]
        public async Task GetRatesAsync_Should_Authenticate_With_The_Live_Credentials_Outside_Test_Mode()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressSettings settings = LiveSettings();
            settings.TestUsername = "test-user";
            settings.TestPassword = "test-pass";
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), settings);

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest());

            // asserts
            HttpRequestMessage request = handler.Requests.Single();
            request.Headers.Authorization!.Scheme.Should().Be("Basic");
            DecodeBasic(request).Should().Be("live-user:live-pass");
        }

        [Fact]
        public async Task GetRatesAsync_Should_Accept_Json()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest());

            // asserts
            handler.Requests.Single().Headers.Accept.Select(x => x.MediaType).Should().Equal("application/json");
        }

        [Fact]
        public async Task GetRatesAsync_Should_Send_The_Message_Reference_Header()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest(), "order-1");

            // asserts
            handler.Requests.Single().Headers.GetValues("Message-Reference").Should().Equal("order-1");
        }

        [Fact]
        public async Task GetRatesAsync_Should_Replace_The_Message_Reference_Header_On_Later_Calls()
        {
            // preparation
            StubHandler handler = new();
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest(), "order-1");
            await client.GetRatesAsync(new DhlExpressRatesRequest(), "order-2");

            // asserts
            handler.Requests[1].Headers.GetValues("Message-Reference").Should().Equal("order-2");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetRatesAsync_Should_Remove_The_Message_Reference_Header_When_No_Reference_Is_Given(string? reference)
        {
            // preparation
            StubHandler handler = new();
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());
            await client.GetRatesAsync(new DhlExpressRatesRequest(), "order-1");

            // execution
            await client.GetRatesAsync(new DhlExpressRatesRequest(), reference!);

            // asserts
            handler.Requests[1].Headers.Contains("Message-Reference").Should().BeFalse();
        }

        [Fact]
        public async Task GetRatesAsync_Should_Post_The_Request_As_Json_And_Read_The_Response()
        {
            // preparation
            StubHandler handler = new() { ResponseJson = DhlExpressRatesResponseTestData.RatesJson };
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());
            DhlExpressRatesRequest request = new() { NextBusinessDay = true };

            // execution
            DhlExpressRatesResponse resp = await client.GetRatesAsync(request);

            // asserts
            handler.RequestBodies.Single().Should().Contain("\"nextBusinessDay\":true");
            handler.Requests.Single().Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
            resp.Products.Select(x => x.ProductCode).Should().Equal("P", "H");
        }

        [Fact]
        public async Task GetRatesAsync_Should_Read_Error_Responses_Into_The_Response_Model()
        {
            // preparation
            StubHandler handler = new() { StatusCode = HttpStatusCode.NotFound, ResponseJson = DhlExpressRatesResponseTestData.ErrorJson };
            DhlExpressClient client = DhlExpressClient.Create(new StubHttpClientFactory(handler), TestSettings());

            // execution
            DhlExpressRatesResponse resp = await client.GetRatesAsync(new DhlExpressRatesRequest());

            // asserts
            resp.Status.Should().Be("404");
            resp.Detail.Should().Be("Unable to find any matching product");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Create_Should_Require_The_Test_Username_In_Test_Mode(string? username)
        {
            // preparation
            DhlExpressSettings settings = TestSettings();
            settings.TestUsername = username!;

            // execution
            Action act = () => DhlExpressClient.Create(new StubHttpClientFactory(new StubHandler()), settings);

            // asserts
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Create_Should_Require_The_Test_Password_In_Test_Mode(string? password)
        {
            // preparation
            DhlExpressSettings settings = TestSettings();
            settings.TestPassword = password!;

            // execution
            Action act = () => DhlExpressClient.Create(new StubHttpClientFactory(new StubHandler()), settings);

            // asserts
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Create_Should_Require_The_Live_Username_Outside_Test_Mode(string? username)
        {
            // preparation
            DhlExpressSettings settings = LiveSettings();
            settings.LiveUsername = username!;

            // execution
            Action act = () => DhlExpressClient.Create(new StubHttpClientFactory(new StubHandler()), settings);

            // asserts
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Create_Should_Require_The_Live_Password_Outside_Test_Mode(string? password)
        {
            // preparation
            DhlExpressSettings settings = LiveSettings();
            settings.LivePassword = password!;

            // execution
            Action act = () => DhlExpressClient.Create(new StubHttpClientFactory(new StubHandler()), settings);

            // asserts
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Not_Require_Live_Credentials_In_Test_Mode()
        {
            // execution
            Action act = () => DhlExpressClient.Create(new StubHttpClientFactory(new StubHandler()), TestSettings());

            // asserts
            act.Should().NotThrow();
        }

        private static DhlExpressSettings TestSettings()
            => new() { TestMode = true, TestUsername = "test-user", TestPassword = "test-pass" };

        private static DhlExpressSettings LiveSettings()
            => new() { TestMode = false, LiveUsername = "live-user", LivePassword = "live-pass" };

        private static string DecodeBasic(HttpRequestMessage request)
            => Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization!.Parameter!));

        private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            public List<HttpRequestMessage> Requests { get; } = [];

            public List<string> RequestBodies { get; } = [];

            public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

            public string ResponseJson { get; init; } = """{ "products": [] }""";

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                RequestBodies.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

                return new HttpResponseMessage(StatusCode)
                {
                    Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
