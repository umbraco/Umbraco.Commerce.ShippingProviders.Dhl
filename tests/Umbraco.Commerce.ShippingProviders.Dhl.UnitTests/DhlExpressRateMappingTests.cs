using FluentAssertions;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.ShippingProviders;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Dhl.UnitTests
{
    public class DhlExpressRateMappingTests
    {
        private static readonly Guid CurrencyId = Guid.NewGuid();
        private static readonly Guid PackageId = Guid.NewGuid();

        [Fact]
        public void MapRates_Should_Split_BILLC_Breakdown_Into_Net_And_Tax()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("P", "EXPRESS WORLDWIDE", ("BILLC", [("SPRQT", 330m), ("STTXA", 82.5m)])));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Value.WithoutTax.Should().Be(330m);
            rate.Value.Tax.Should().Be(82.5m);
            rate.Value.WithTax.Should().Be(412.5m);
            rate.Value.CurrencyId.Should().Be(CurrencyId);
        }

        [Fact]
        public void MapRates_Should_Ignore_Non_BILLC_Breakdowns()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct(
                "P",
                "EXPRESS WORLDWIDE",
                ("PULCL", [("SPRQT", 999m), ("STTXA", 111m)]),
                ("BILLC", [("SPRQT", 330m), ("STTXA", 82.5m)])));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Value.WithoutTax.Should().Be(330m);
            rate.Value.Tax.Should().Be(82.5m);
        }

        [Fact]
        public void MapRates_Should_Use_Zero_When_There_Is_No_BILLC_Breakdown()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("P", "EXPRESS WORLDWIDE", ("PULCL", [("SPRQT", 999m), ("STTXA", 111m)])));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Value.WithoutTax.Should().Be(0m);
            rate.Value.Tax.Should().Be(0m);
            rate.Value.WithTax.Should().Be(0m);
        }

        [Fact]
        public void MapRates_Should_Use_Zero_Tax_When_STTXA_Is_Missing()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("P", "EXPRESS WORLDWIDE", ("BILLC", [("SPRQT", 330m)])));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Value.WithoutTax.Should().Be(330m);
            rate.Value.Tax.Should().Be(0m);
        }

        [Fact]
        public void MapRates_Should_Use_Zero_Net_When_SPRQT_Is_Missing()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("P", "EXPRESS WORLDWIDE", ("BILLC", [("STTXA", 82.5m)])));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Value.WithoutTax.Should().Be(0m);
            rate.Value.Tax.Should().Be(82.5m);
        }

        [Fact]
        public void MapRates_Should_Use_Zero_When_There_Is_No_Breakdown_At_All()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("P", "EXPRESS WORLDWIDE"));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Value.WithTax.Should().Be(0m);
        }

        [Theory]
        [InlineData("P", "EXPRESS WORLDWIDE")]
        [InlineData("H", "ECONOMY SELECT")]
        [InlineData("K", "EXPRESS 9:00")]
        [InlineData("N", "EXPRESS DOMESTIC")]
        [InlineData("PX", "EXPRESS WORLDWIDE")] // only the first character selects the service
        public void MapRates_Should_Name_The_Service_From_The_First_Character_Of_The_Product_Code(string productCode, string expectedName)
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct(productCode, "Name From DHL"));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Option.Name.Should().Be(expectedName);
        }

        [Fact]
        public void MapRates_Should_Fall_Back_To_The_Product_Name_For_Unknown_Product_Codes()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("Z", "Name From DHL"));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Option.Name.Should().Be("Name From DHL");
        }

        [Fact]
        public void MapRates_Should_Use_The_Full_Product_Code_As_Option_Id_And_Attach_The_Package()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("PX", "Name From DHL"));

            // execution
            ShippingRate rate = MapSingle(resp);

            // asserts
            rate.Option.Id.Should().Be("PX");
            rate.PackageId.Should().Be(PackageId);
        }

        [Fact]
        public void MapRates_Should_Return_One_Rate_Per_Product_In_Order()
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(
                CreateProduct("P", "A", ("BILLC", [("SPRQT", 10m), ("STTXA", 2m)])),
                CreateProduct("H", "B", ("BILLC", [("SPRQT", 20m), ("STTXA", 5m)])));

            // execution
            ShippingRatesResult result = DhlExpressShippingProvider.MapRates(resp, CurrencyId, PackageId);

            // asserts
            ShippingRate[] rates = result.Rates.ToArray();
            rates.Select(x => x.Option.Id).Should().Equal("P", "H");
            rates.Select(x => x.Value.WithTax).Should().Equal(12m, 25m);
        }

        [Theory]
        [InlineData("400")]
        [InlineData("404")]
        [InlineData("500")]
        [InlineData("")]
        public void MapRates_Should_Return_Empty_When_Status_Is_Not_200(string status)
        {
            // preparation
            DhlExpressRatesResponse resp = CreateResponse(CreateProduct("P", "EXPRESS WORLDWIDE"));
            resp.Status = status;

            // execution
            ShippingRatesResult result = DhlExpressShippingProvider.MapRates(resp, CurrencyId, PackageId);

            // asserts
            result.Should().Be(ShippingRatesResult.Empty);
        }

        [Fact]
        public void MapRates_Should_Map_A_Deserialized_DHL_Response()
        {
            // preparation
            DhlExpressRatesResponse resp = DhlExpressRatesSerializationTests.Deserialize(DhlExpressRatesResponseTestData.RatesJson);

            // execution
            ShippingRatesResult result = DhlExpressShippingProvider.MapRates(resp, CurrencyId, PackageId);

            // asserts
            ShippingRate[] rates = result.Rates.ToArray();
            rates.Should().HaveCount(2);
            rates[0].Option.Id.Should().Be("P");
            rates[0].Option.Name.Should().Be("EXPRESS WORLDWIDE");
            rates[0].Value.WithoutTax.Should().Be(330m);
            rates[0].Value.Tax.Should().Be(82.5m);
            rates[1].Option.Id.Should().Be("H");
            rates[1].Option.Name.Should().Be("ECONOMY SELECT");
            rates[1].Value.WithTax.Should().Be(250m);
        }

        private static ShippingRate MapSingle(DhlExpressRatesResponse resp)
        {
            ShippingRatesResult result = DhlExpressShippingProvider.MapRates(resp, CurrencyId, PackageId);
            return result.Rates.Should().ContainSingle().Subject;
        }

        private static DhlExpressRatesResponse CreateResponse(params DhlExpressProduct[] products)
            => new() { Products = [.. products] };

        private static DhlExpressProduct CreateProduct(string code, string name, params (string CurrencyType, (string TypeCode, decimal Price)[] Items)[] breakdowns)
        {
            DhlExpressProduct product = new() { ProductCode = code, ProductName = name };

            foreach ((string currencyType, (string TypeCode, decimal Price)[] items) in breakdowns)
            {
                product.TotalPriceBreakdown.Add(new DhlExpressTotalPriceWithBreakdown
                {
                    CurrencyType = currencyType,
                    PriceCurrency = "DKK",
                    PriceBreakdown = items.Select(x => new DhlExpressPriceBreakdown { TypeCode = x.TypeCode, Price = x.Price }).ToList(),
                });
            }

            return product;
        }
    }
}
