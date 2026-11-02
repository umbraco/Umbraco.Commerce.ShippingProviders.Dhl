using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;
using System.Net.Http.Json;
using System.Threading;

namespace Umbraco.Commerce.ShippingProviders.Dhl.Api
{
    public class DhlExpressClient
    {
        // The same defaults ReadFromJsonAsync uses, so the response models deserialize exactly as before
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private const int MaxLoggedBodyLength = 1000;

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
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                return ParseRatesResponse(resp.StatusCode, body);
            }
        }

        /// <summary>
        /// Turns a DHL rates response into the response model without throwing. A failed or unreadable response
        /// comes back with <see cref="DhlExpressResponseBase.Status"/> set to the HTTP status code and, when the
        /// body isn't a DHL error, the (truncated) raw body in <see cref="DhlExpressResponseBase.Detail"/>.
        /// </summary>
        internal static DhlExpressRatesResponse ParseRatesResponse(HttpStatusCode statusCode, string? body)
        {
            var status = ((int)statusCode).ToString(CultureInfo.InvariantCulture);
            var isSuccess = (int)statusCode >= 200 && (int)statusCode <= 299;

            DhlExpressRatesResponse? parsed = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    parsed = JsonSerializer.Deserialize<DhlExpressRatesResponse>(body, JsonOptions);
                }
                catch (JsonException)
                {
                    // Not JSON, or not in the shape we expect (a gateway error page, for example)
                }
            }

            if (isSuccess)
            {
                if (parsed?.Products != null)
                {
                    return parsed;
                }

                // A success status without any products is a response we can't use
                return new DhlExpressRatesResponse
                {
                    Status = status,
                    Message = "The DHL rates response contained no products",
                    Detail = parsed == null ? Truncate(body) : parsed.Detail,
                };
            }

            if (parsed != null && !(string.IsNullOrWhiteSpace(parsed.Title) && string.IsNullOrWhiteSpace(parsed.Message) && string.IsNullOrWhiteSpace(parsed.Detail)))
            {
                // DHL error bodies carry their own title, message and detail, but the HTTP status is authoritative
                parsed.Status = status;
                parsed.Detail = Truncate(parsed.Detail);
                return parsed;
            }

            return new DhlExpressRatesResponse
            {
                Status = status,
                Message = "DHL returned an unreadable error response",
                Detail = Truncate(body),
            };
        }

        private static string Truncate(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length > MaxLoggedBodyLength
                ? string.Concat(value.AsSpan(0, MaxLoggedBodyLength), "...")
                : value;
        }

    }
}
