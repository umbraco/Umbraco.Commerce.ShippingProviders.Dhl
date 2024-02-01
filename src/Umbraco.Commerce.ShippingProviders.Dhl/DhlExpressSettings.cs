using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.Dhl
{
    public class DhlExpressSettings
    {
        [ShippingProviderSetting(Name = "Account Number",
            Description = "Your DHL account number.",
            SortOrder = 200)]
        public string AccountNumber { get; set; }

        [ShippingProviderSetting(Name = "Shipping Timeframe",
            Description = "The number of days after an order is placed you generally ship by. 0 = same day, 1 = next day, etc",
            SortOrder = 205)]
        public int ShippingTimeframe { get; set; }

        [ShippingProviderSetting(Name = "Next Business Day Fallback",
            Description = "When set to true and there are no products available within the shipping timeframe then products available for the next possible pickup date are returned.",
            SortOrder = 206)]
        public bool NextBusinessDayFallback { get; set; }

        [ShippingProviderSetting(Name = "Test Username",
            Description = "The Test Username from the DHL portal.",
            SortOrder = 210)]
        public string TestUsername { get; set; }

        [ShippingProviderSetting(Name = "Test Password",
            Description = "The Test Password from the DHL portal.",
            SortOrder = 220)]
        public string TestPassword { get; set; }

        [ShippingProviderSetting(Name = "Live Username",
            Description = "The Live Username from the DHL portal.",
            SortOrder = 230)]
        public string LiveUsername { get; set; }

        [ShippingProviderSetting(Name = "Live Password",
            Description = "The Live Password from the DHL portal.",
            SortOrder = 240)]
        public string LivePassword { get; set; }

        [ShippingProviderSetting(Name = "Test Mode",
            Description = "Set whether to run in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }


        [ShippingProviderSetting(Name = "Product Codes",
            Description = "Comma-seperated list of product codes to limit the lookup to.",
            SortOrder = 10100,
            IsAdvanced = true)]
        public string ProductCodes { get; set; }
    }
}
