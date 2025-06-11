using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.Dhl
{
    public class DhlExpressSettings
    {
        [ShippingProviderSetting(SortOrder = 200)]
        public string AccountNumber { get; set; }

        [ShippingProviderSetting(SortOrder = 205)]
        public int ShippingTimeframe { get; set; }

        [ShippingProviderSetting(SortOrder = 206)]
        public bool NextBusinessDayFallback { get; set; }

        [ShippingProviderSetting(SortOrder = 210)]
        public string TestUsername { get; set; }

        [ShippingProviderSetting(SortOrder = 220)]
        public string TestPassword { get; set; }

        [ShippingProviderSetting(SortOrder = 230)]
        public string LiveUsername { get; set; }

        [ShippingProviderSetting(SortOrder = 240)]
        public string LivePassword { get; set; }

        [ShippingProviderSetting(SortOrder = 10000)]
        public bool TestMode { get; set; }


        [ShippingProviderSetting(SortOrder = 10100, IsAdvanced = true)]
        public string ProductCodes { get; set; }
    }
}
