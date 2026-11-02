using FluentAssertions;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.ShippingProviders.Dhl.Api.Models;

namespace Umbraco.Commerce.ShippingProviders.Dhl.UnitTests
{
    public class DhlExpressRatesRequestTests
    {
        private static readonly DateTime Now = new(2026, 11, 2, 14, 30, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("DK", "DK", false)] // domestic
        [InlineData("US", "US", false)] // domestic outside the EU
        [InlineData("DK", "DE", false)] // both in the EU
        [InlineData("SE", "FR", false)]
        [InlineData("DK", "GB", true)] // EU to non-EU
        [InlineData("GB", "DK", true)] // non-EU to EU
        [InlineData("DK", "US", true)]
        [InlineData("GB", "US", true)]
        public void IsCustomsDeclarable_Should_Be_False_For_Domestic_And_Intra_EU_Only(string sender, string receiver, bool expected)
        {
            // execution
            bool result = DhlExpressShippingProvider.IsCustomsDeclarable(sender, receiver);

            // asserts
            result.Should().Be(expected);
        }

        [Fact]
        public void BuildRatesRequest_Should_Map_Addresses()
        {
            // preparation
            Package package = CreatePackage();

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(package, new DhlExpressSettings(), MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.CustomerDetails.ShipperDetails.Should().BeEquivalentTo(new DhlExpressAddress { CityName = "Copenhagen", PostalCode = "1000", CountryCode = "DK" });
            request.CustomerDetails.ReceiverDetails.Should().BeEquivalentTo(new DhlExpressAddress { CityName = "New York", PostalCode = "10001", CountryCode = "US" });
        }

        [Theory]
        [InlineData("DK", "DK", false)]
        [InlineData("DK", "DE", false)]
        [InlineData("DK", "US", true)]
        public void BuildRatesRequest_Should_Set_Customs_Flag_From_Countries(string sender, string receiver, bool expected)
        {
            // preparation
            Package package = CreatePackage(sender, receiver);

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(package, new DhlExpressSettings(), MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.IsCustomsDeclarable.Should().Be(expected);
        }

        [Fact]
        public void BuildRatesRequest_Should_Set_Account_Number_As_Shipper_Account()
        {
            // preparation
            DhlExpressSettings settings = new() { AccountNumber = "123456789" };

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), settings, MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            DhlExpressAccount account = request.Accounts.Should().ContainSingle().Subject;
            account.Number.Should().Be("123456789");
            account.TypeCode.Should().Be("shipper");
        }

        [Fact]
        public void BuildRatesRequest_Should_Set_Declared_Value_From_Subtotal_And_Currency()
        {
            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), new DhlExpressSettings(), MeasurementSystem.Metric, 1250.75m, "DKK", Now);

            // asserts
            DhlExpressMonetaryAmount amount = request.MonetaryAmount.Should().ContainSingle().Subject;
            amount.Value.Should().Be(1250.75m);
            amount.Currency.Should().Be("DKK");
            amount.TypeCode.Should().Be("declaredValue");
        }

        [Theory]
        [InlineData(MeasurementSystem.Metric, "metric")]
        [InlineData(MeasurementSystem.Imperial, "imperial")]
        public void BuildRatesRequest_Should_Lowercase_Measurement_System_As_Unit_Of_Measurement(MeasurementSystem system, string expected)
        {
            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), new DhlExpressSettings(), system, 100m, "DKK", Now);

            // asserts
            request.UnitOfMeasurement.Should().Be(expected);
        }

        [Fact]
        public void BuildRatesRequest_Should_Map_Package_Weight_And_Dimensions()
        {
            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), new DhlExpressSettings(), MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            DhlExpressPackage package = request.Packages.Should().ContainSingle().Subject;
            package.Weight.Should().Be(2.5m);
            package.Dimensions.Length.Should().Be(30m);
            package.Dimensions.Width.Should().Be(20m);
            package.Dimensions.Height.Should().Be(10m);
        }

        [Theory]
        [InlineData(0, "2026-11-02")]
        [InlineData(3, "2026-11-05")]
        [InlineData(30, "2026-12-02")]
        public void BuildRatesRequest_Should_Offset_Planned_Date_By_Shipping_Timeframe_From_Start_Of_Day(int timeframe, string expectedDate)
        {
            // preparation
            DhlExpressSettings settings = new() { ShippingTimeframe = timeframe };

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), settings, MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.PlannedShippingDateAndTime.Should().Be(DateTime.Parse(expectedDate, System.Globalization.CultureInfo.InvariantCulture).Date);
            request.PlannedShippingDateAndTime.TimeOfDay.Should().Be(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BuildRatesRequest_Should_Map_Next_Business_Day_Fallback(bool fallback)
        {
            // preparation
            DhlExpressSettings settings = new() { NextBusinessDayFallback = fallback };

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), settings, MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.NextBusinessDay.Should().Be(fallback);
        }

        [Fact]
        public void BuildRatesRequest_Should_Split_Product_Codes_On_Comma()
        {
            // preparation
            DhlExpressSettings settings = new() { ProductCodes = "P,D,K" };

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), settings, MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.ProductsAndServices.Select(x => x.ProductCode).Should().Equal("P", "D", "K");
        }

        [Fact]
        public void BuildRatesRequest_Should_Skip_Empty_Product_Code_Entries()
        {
            // preparation
            DhlExpressSettings settings = new() { ProductCodes = "P,,D," };

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), settings, MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.ProductsAndServices.Select(x => x.ProductCode).Should().Equal("P", "D");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BuildRatesRequest_Should_Send_No_Products_When_Product_Codes_Are_Blank(string? productCodes)
        {
            // preparation
            DhlExpressSettings settings = new() { ProductCodes = productCodes! };

            // execution
            DhlExpressRatesRequest request = DhlExpressShippingProvider.BuildRatesRequest(CreatePackage(), settings, MeasurementSystem.Metric, 100m, "DKK", Now);

            // asserts
            request.ProductsAndServices.Should().BeEmpty();
        }

        private static Package CreatePackage(string senderCountry = "DK", string receiverCountry = "US")
            => new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Address("Sender Street 1", "Copenhagen", senderCountry, "1000"),
                new Address("Receiver Street 1", "New York", receiverCountry, "10001"),
                Guid.NewGuid(),
                [],
                30m,
                20m,
                10m,
                2.5m);
    }
}
