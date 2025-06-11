using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;
using System.Net.Http.Json;
using System.Threading;

namespace Umbraco.Commerce.ShippingProviders.Dhl.Api
{
    public class DhlExpressClient
    {
        private readonly HttpClient _httpClient;
        private readonly DhlExpressSettings _settings;

        public static DhlExpressClient Create(IHttpClientFactory httpClientFactory, DhlExpressSettings settings)
            => new DhlExpressClient(httpClientFactory.CreateClient(), settings);

        private DhlExpressClient(HttpClient httpClient, DhlExpressSettings settings)
        {
            settings.MustNotBeNull(nameof(settings));

            string baseAddress;
            string username;
            string password;

            if (settings.TestMode)
            {
                settings.TestUsername.MustNotBeNullOrWhiteSpace(nameof(settings.TestUsername));
                settings.TestPassword.MustNotBeNullOrWhiteSpace(nameof(settings.TestPassword));

                baseAddress = "https://express.api.dhl.com/mydhlapi/test/";
                username = settings.TestUsername;
                password = settings.TestPassword;
            }
            else
            {
                settings.LiveUsername.MustNotBeNullOrWhiteSpace(nameof(settings.LiveUsername));
                settings.LivePassword.MustNotBeNullOrWhiteSpace(nameof(settings.LivePassword));

                baseAddress = "https://express.api.dhl.com/mydhlapi/";
                username = settings.LiveUsername;
                password = settings.LivePassword;
            }

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(baseAddress);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{username}:{password}".Base64Encode());

            _settings = settings;
        }

        public async Task<DhlExpressRatesResponse> GetRatesAsync(DhlExpressRatesRequest req, string messageReference = null, CancellationToken cancellationToken = default)
        {
            _httpClient.DefaultRequestHeaders.Remove("Message-Reference");

            if (!string.IsNullOrEmpty(messageReference))
            {
                _httpClient.DefaultRequestHeaders.Add("Message-Reference", messageReference);
            }

            using (var resp = await _httpClient.PostAsJsonAsync("rates", req, cancellationToken).ConfigureAwait(false))
            {
                return await resp.Content.ReadFromJsonAsync<DhlExpressRatesResponse>(cancellationToken).ConfigureAwait(false);
            }
        }

    }
}
