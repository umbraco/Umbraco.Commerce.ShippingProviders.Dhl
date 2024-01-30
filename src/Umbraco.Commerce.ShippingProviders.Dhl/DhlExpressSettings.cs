using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.Shipmondo
{
    public class DhlExpressSettings
    {
        [ShippingProviderSetting(Name = "Test Username",
            Description = "The Test Username from the DHL portal.",
            SortOrder = 200)]
        public string TestUsername { get; set; }

        [ShippingProviderSetting(Name = "Test Password",
            Description = "The Test Password from the DHL portal.",
            SortOrder = 210)]
        public string TestPassword { get; set; }

        [ShippingProviderSetting(Name = "Live Username",
            Description = "The Live Username from the DHL portal.",
            SortOrder = 220)]
        public string LiveUsername { get; set; }

        [ShippingProviderSetting(Name = "Live Password",
            Description = "The Live Password from the DHL portal.",
            SortOrder = 220)]
        public string LivePassword { get; set; }

        [ShippingProviderSetting(Name = "Test Mode",
            Description = "Set whether to run in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
