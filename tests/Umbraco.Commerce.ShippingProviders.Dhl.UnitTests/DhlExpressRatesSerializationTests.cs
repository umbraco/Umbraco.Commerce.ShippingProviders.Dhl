using System.Text.Json;
using FluentAssertions;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Dhl.UnitTests
{
    public class DhlExpressRatesSerializationTests
    {
        // Same options as HttpClient's PostAsJsonAsync / ReadFromJsonAsync
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        internal static DhlExpressRatesResponse Deserialize(string json)
            => JsonSerializer.Deserialize<DhlExpressRatesResponse>(json, Options)!;

        [Fact]
        public void Request_Should_Serialize_With_The_Property_Names_DHL_Expects()
        {
            // preparation
            DhlExpressSettings settings = new()
            {
                AccountNumber = "123456789",
                NextBusinessDayFallback = true,
                ProductCodes = "P,D",
            };
            Package package = new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Address("Sender Street 1", "Copenhagen", "DK", "1000"),
                new Address("Receiver Street 1", "New York", "US", "10001"),
                Guid.NewGuid(),
                [],
                30m,
                20m,
                10m,
                2.5m);
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(package, settings, MeasurementSystem.Metric, 412.5m, "DKK", new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));

            // execution
            using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(request, Options));
            JsonElement root = json.RootElement;

            // asserts
            root.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo(
                "customerDetails", "unitOfMeasurement", "monetaryAmount", "accounts", "packages", "plannedShippingDateAndTime", "nextBusinessDay", "productTypeCode", "productsAndServices", "isCustomsDeclarable");
            root.GetProperty("productTypeCode").GetString().Should().Be("all");
            root.GetProperty("unitOfMeasurement").GetString().Should().Be("metric");
            root.GetProperty("nextBusinessDay").GetBoolean().Should().BeTrue();
            root.GetProperty("isCustomsDeclarable").GetBoolean().Should().BeTrue();

            JsonElement shipper = root.GetProperty("customerDetails").GetProperty("shipperDetails");
            shipper.GetProperty("postalCode").GetString().Should().Be("1000");
            shipper.GetProperty("cityName").GetString().Should().Be("Copenhagen");
            shipper.GetProperty("countryCode").GetString().Should().Be("DK");
            JsonElement receiver = root.GetProperty("customerDetails").GetProperty("receiverDetails");
            receiver.GetProperty("postalCode").GetString().Should().Be("10001");
            receiver.GetProperty("cityName").GetString().Should().Be("New York");
            receiver.GetProperty("countryCode").GetString().Should().Be("US");

            JsonElement account = root.GetProperty("accounts")[0];
            account.GetProperty("typeCode").GetString().Should().Be("shipper");
            account.GetProperty("number").GetString().Should().Be("123456789");

            JsonElement amount = root.GetProperty("monetaryAmount")[0];
            amount.GetProperty("typeCode").GetString().Should().Be("declaredValue");
            amount.GetProperty("value").GetDecimal().Should().Be(412.5m);
            amount.GetProperty("currency").GetString().Should().Be("DKK");

            JsonElement packageJson = root.GetProperty("packages")[0];
            packageJson.GetProperty("weight").GetDecimal().Should().Be(2.5m);
            JsonElement dimensions = packageJson.GetProperty("dimensions");
            dimensions.GetProperty("length").GetDecimal().Should().Be(30m);
            dimensions.GetProperty("width").GetDecimal().Should().Be(20m);
            dimensions.GetProperty("height").GetDecimal().Should().Be(10m);

            root.GetProperty("productsAndServices").EnumerateArray().Select(x => x.GetProperty("productCode").GetString()).Should().Equal("P", "D");
            DateTime.Parse(root.GetProperty("plannedShippingDateAndTime").GetString()!, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime().Date.Should().Be(new DateTime(2026, 11, 2));
        }

        [Fact]
        public void Response_Should_Deserialize_A_Realistic_DHL_Rates_Response()
        {
            // execution
            DhlExpressRatesResponse resp = Deserialize(DhlExpressRatesResponseTestData.RatesJson);

            // asserts
            resp.Status.Should().Be("200");
            resp.Products.Should().HaveCount(2);

            DhlExpressProduct first = resp.Products[0];
            first.ProductName.Should().Be("EXPRESS WORLDWIDE");
            first.ProductCode.Should().Be("P");
            first.TotalPrice.Should().HaveCount(2);
            first.TotalPrice[0].CurrencyType.Should().Be("BILLC");
            first.TotalPrice[0].PriceCurrency.Should().Be("DKK");
            first.TotalPrice[0].Price.Should().Be(412.5m);
            first.TotalPriceBreakdown.Should().HaveCount(2);
            DhlExpressTotalPriceWithBreakdown billc = first.TotalPriceBreakdown[0];
            billc.CurrencyType.Should().Be("BILLC");
            billc.PriceCurrency.Should().Be("DKK");
            billc.PriceBreakdown.Select(x => (x.TypeCode, x.Price)).Should().Equal(("SPRQT", 330m), ("STTXA", 82.5m));
        }

        [Fact]
        public void Response_Should_Default_Status_To_200_When_DHL_Sends_None()
        {
            // execution
            DhlExpressRatesResponse resp = Deserialize("""{ "products": [] }""");

            // asserts
            resp.Status.Should().Be("200");
            resp.Products.Should().BeEmpty();
        }

        [Fact]
        public void Response_Should_Deserialize_A_DHL_Error_Response()
        {
            // execution
            DhlExpressRatesResponse resp = Deserialize(DhlExpressRatesResponseTestData.ErrorJson);

            // asserts
            resp.Status.Should().Be("404");
            resp.Title.Should().Be("Not Found");
            resp.Message.Should().Be("No products found for the requested route");
            resp.Detail.Should().Be("Unable to find any matching product");
        }
    }
}
