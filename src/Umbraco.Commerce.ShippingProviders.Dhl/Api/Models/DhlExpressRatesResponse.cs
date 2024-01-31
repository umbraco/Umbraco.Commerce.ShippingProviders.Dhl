using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Umbraco.Commerce.ShippingProviders.Dhl.Api.Models
{
    public class DhlExpressResponseBase
    {
        public string Location { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
        public string Status { get; set; }
        public IEnumerable<string> AdditionalDetails { get; set; }

        public DhlExpressResponseBase()
        {
            Status = "200";
        }
    }

    public class DhlExpressRatesResponse : DhlExpressResponseBase
    {
        [JsonPropertyName("products")]
        public List<DhlExpressProduct> Products { get; set; }
    }

    public class DhlExpressProduct
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; }

        [JsonPropertyName("totalPrice")]
        public List<DhlExpressTotalPrice> TotalPrice { get; set; }

        [JsonPropertyName("totalPriceBreakdown")]
        public List<DhlExpressTotalPriceWithBreakdown> TotalPriceBreakdown { get; set; }

        public DhlExpressProduct()
        {
            TotalPrice = new List<DhlExpressTotalPrice>();
            TotalPriceBreakdown = new List<DhlExpressTotalPriceWithBreakdown>();
        }
    }

    public class DhlExpressTotalPrice
    {
        [JsonPropertyName("currencyType")]
        public string CurrencyType { get; set; }

        [JsonPropertyName("priceCurrency")]
        public string PriceCurrency { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class DhlExpressTotalPriceWithBreakdown
    {
        [JsonPropertyName("currencyType")]
        public string CurrencyType { get; set; }

        [JsonPropertyName("priceCurrency")]
        public string PriceCurrency { get; set; }

        [JsonPropertyName("priceBreakdown")]
        public List<DhlExpressPriceBreakdown> PriceBreakdown { get; set; }

        public DhlExpressTotalPriceWithBreakdown()
        {
            PriceBreakdown = new List<DhlExpressPriceBreakdown>();
        }
    }

    public class DhlExpressPriceBreakdown
    {
        [JsonPropertyName("typeCode")]
        public string TypeCode { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
