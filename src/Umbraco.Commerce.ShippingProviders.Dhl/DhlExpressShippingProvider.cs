using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.ShippingProviders;
using Umbraco.Commerce.Extensions;
using Umbraco.Commerce.ShippingProviders.Dhl.Api;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Dhl
{
    [ShippingProvider("dhlexpress")]
    public class DhlExpressShippingProvider(
        UmbracoCommerceContext ctx,
        IHttpClientFactory httpClientFactory,
        ILogger<DhlExpressShippingProvider> logger)
        : ShippingProviderBase<DhlExpressSettings>(ctx)
    {
        private static string[] EuCountryCodes => new[]
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "EL", "HU",
            "IE", "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE"
        };

        private static Dictionary<char, string> AvailableServices => new Dictionary<char, string>
        {
            { '1', "EXPRESS DOMESTIC 12:00" },
            { '4', "JETLINE" },
            { '5', "SPRINTLINE" },
            { '7', "EXPRESS EASY" },
            { '8', "EXPRESS EASY" },
            { 'B', "EXPRESS BREAKBULK" },
            { 'C', "MEDICAL EXPRESS" },
            { 'D', "EXPRESS WORLDWIDE" },
            { 'E', "EXPRESS 9:00" },
            { 'F', "FREIGHT WORLDWIDE" },
            { 'G', "DOMESTIC ECONOMY SELECT" },
            { 'H', "ECONOMY SELECT" },
            { 'I', "EXPRESS DOMESTIC 9:00" },
            { 'J', "JUMBO BOX" },
            { 'K', "EXPRESS 9:00" },
            { 'L', "EXPRESS 10:30" },
            { 'M', "EXPRESS 10:30" },
            { 'N', "EXPRESS DOMESTIC" },
            { 'O', "EXPRESS DOMESTIC 10:30" },
            { 'P', "EXPRESS WORLDWIDE" },
            { 'Q', "MEDICAL EXPRESS" },
            { 'R', "GLOBALMAIL BUSINESS" },
            { 'S', "SAME DAY" },
            { 'T', "EXPRESS 12:00" },
            { 'U', "EXPRESS WORLDWIDE" },
            { 'V', "EUROPACK" },
            { 'W', "ECONOMY SELECT" },
            { 'X', "EXPRESS ENVELOPE" },
            { 'Y', "EXPRESS 12:00" }
        };

        public override bool SupportsRealtimeRates => true;

        public override async Task<ShippingRatesResult> GetShippingRatesAsync(ShippingProviderContext<DhlExpressSettings> context, CancellationToken cancellationToken = default)
        {
            var package = context.Packages.FirstOrDefault();

            if (package == null || !package.HasMeasurements)
            {
                logger.Debug("Unable to calculate realtime DHL rates as the package provided is invalid");
                return ShippingRatesResult.Empty;
            }

            var client = DhlExpressClient.Create(httpClientFactory, context.Settings);

            // Assume cross customs border by default
            var isCustomsDeclarable = true;

            if (package.SenderAddress.CountryIsoCode == package.ReceiverAddress.CountryIsoCode)
            {
                // Domestic
                isCustomsDeclarable = false;
            }
            else if (EuCountryCodes.InvariantContains(package.SenderAddress.CountryIsoCode) && EuCountryCodes.InvariantContains(package.ReceiverAddress.CountryIsoCode))
            {
                // Inside the EU
                isCustomsDeclarable = false;
            }

            var request = new DhlExpressRatesRequest
            {
                CustomerDetails = new DhlExpressCustomerDetails
                {
                    ReceiverDetails = new DhlExpressAddress
                    {
                        CityName = package.ReceiverAddress.City,
                        PostalCode = package.ReceiverAddress.ZipCode,
                        CountryCode = package.ReceiverAddress.CountryIsoCode
                    },
                    ShipperDetails = new DhlExpressAddress
                    {
                        CityName = package.SenderAddress.City,
                        PostalCode = package.SenderAddress.ZipCode,
                        CountryCode = package.SenderAddress.CountryIsoCode
                    }
                },
                IsCustomsDeclarable = isCustomsDeclarable
            };

            request.Accounts.Add(new DhlExpressAccount
            {
                Number = context.Settings.AccountNumber
            });

            request.UnitOfMeasurement = context.MeasurementSystem.ToString().ToLowerInvariant();
            request.Packages.Add(new DhlExpressPackage
            {
                Weight = package.Weight,
                Dimensions = new DhlExpressDimensions
                {
                    Length = package.Length,
                    Width = package.Width,
                    Height = package.Height
                }
            });

            var orderCurrency = Context.Services.CurrencyService.GetCurrency(context.Order.CurrencyId);
            request.MonetaryAmount.Add(new DhlExpressMonetaryAmount
            {
                Value = context.Order.SubtotalPrice.Value.WithTax,
                Currency = orderCurrency.Code
            });

            request.PlannedShippingDateAndTime = DateTime.UtcNow.Date.AddDays(context.Settings.ShippingTimeframe);
            request.NextBusinessDay = context.Settings.NextBusinessDayFallback;

            if (!string.IsNullOrWhiteSpace(context.Settings.ProductCodes))
            {
                request.ProductsAndServices.AddRange(context.Settings.ProductCodes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new DhlExpressProductAndServices { ProductCode = x }));
            }

            var resp =  await client.GetRatesAsync(request, context.Order.Id.ToString(), cancellationToken).ConfigureAwait(false);

            if (resp.Status != "200")
            {
                logger.Error($"Failed to get DHL realtime rates: [{resp.Message}] {resp.Detail}");
                return ShippingRatesResult.Empty;
            }

            return new ShippingRatesResult
            {
                Rates = resp.Products.Select(p =>
                {
                    var productPriceBreakdown = p.TotalPriceBreakdown.FirstOrDefault(x => x.CurrencyType == "BILLC");
                    var productPriceTax = productPriceBreakdown?.PriceBreakdown.FirstOrDefault(x => x.TypeCode == "STTXA")?.Price ?? 0;
                    var productPriceNet = productPriceBreakdown?.PriceBreakdown.FirstOrDefault(x => x.TypeCode == "SPRQT")?.Price ?? 0;
                    var productPrice = new Price(productPriceNet, productPriceTax, context.Order.CurrencyId);
                    var productName = AvailableServices.TryGetValue(p.ProductCode[0], out string value) ? value : p.ProductName;

                    var option = new ShippingOption(p.ProductCode, productName);

                    return new ShippingRate(productPrice, option, package.Id);
                }).ToList()
            };
        }
    }
}
