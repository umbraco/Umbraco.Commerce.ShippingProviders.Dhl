using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Umbraco.Commerce.ShippingProviders.Dhl.Api.Models
{
    public class DhlExpressRatesRequest
    {
        [JsonPropertyName("customerDetails")]
        public DhlExpressCustomerDetails CustomerDetails { get; set; }

        [JsonPropertyName("unitOfMeasurement")]
        public string UnitOfMeasurement { get; set; }

        [JsonPropertyName("monetaryAmount")]
        public List<DhlExpressMonetaryAmount> MonetaryAmount { get; set; }

        [JsonPropertyName("accounts")]
        public List<DhlExpressAccount> Accounts { get; set; }

        [JsonPropertyName("packages")]
        public List<DhlExpressPackage> Packages { get; set; }

        [JsonPropertyName("plannedShippingDateAndTime")]
        public DateTime PlannedShippingDateAndTime { get; set; }

        [JsonPropertyName("nextBusinessDay")]
        public bool NextBusinessDay { get; set; }

        [JsonPropertyName("productTypeCode")]
        public string ProductTypeCode { get; set; }

        [JsonPropertyName("productsAndServices")]
        public List<DhlExpressProductAndServices> ProductsAndServices { get; set; }

        [JsonPropertyName("isCustomsDeclarable")]
        public bool IsCustomsDeclarable { get; set; }

        public DhlExpressRatesRequest()
        {
            ProductTypeCode = "all";
            IsCustomsDeclarable = true;

            MonetaryAmount = new List<DhlExpressMonetaryAmount>();
            Accounts = new List<DhlExpressAccount>();
            Packages = new List<DhlExpressPackage>();
            ProductsAndServices = new List<DhlExpressProductAndServices>();
        }
    }

    public class DhlExpressCustomerDetails
    {
        [JsonPropertyName("shipperDetails")]
        public DhlExpressAddress ShipperDetails { get; set; }

        [JsonPropertyName("receiverDetails")]
        public DhlExpressAddress ReceiverDetails { get; set; }
    }

    public class DhlExpressAddress
    {
        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; }

        [JsonPropertyName("cityName")]
        public string CityName { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }
    }

    public class DhlExpressPackage
    {
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("dimensions")]
        public DhlExpressDimensions Dimensions { get; set; }
    }

    public class DhlExpressDimensions
    {
        [JsonPropertyName("length")]
        public decimal Length { get; set; }

        [JsonPropertyName("width")]
        public decimal Width { get; set; }

        [JsonPropertyName("height")]
        public decimal Height { get; set; }
    }

    public class DhlExpressAccount
    {
        [JsonPropertyName("typeCode")]
        public string TypeCode { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        public DhlExpressAccount()
        {
            TypeCode = "shipper";
        }
    }

    public class DhlExpressMonetaryAmount
    {
        [JsonPropertyName("typeCode")]
        public string TypeCode { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        public DhlExpressMonetaryAmount()
        {
            TypeCode = "declaredValue";
        }
    }

    public class DhlExpressProductAndServices
    {
        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; }
    }
}
