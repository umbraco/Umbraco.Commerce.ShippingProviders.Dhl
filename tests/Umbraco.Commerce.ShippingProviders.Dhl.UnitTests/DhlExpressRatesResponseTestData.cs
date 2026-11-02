namespace Umbraco.Commerce.ShippingProviders.Dhl.UnitTests
{
    internal static class DhlExpressRatesResponseTestData
    {
        // Shape of a MyDHL API "rates" response: two products, each with price breakdowns in
        // billing (BILLC) and pickup-country (PULCL) currency, plus fields the provider ignores
        public const string RatesJson = """
            {
              "products": [
                {
                  "productName": "EXPRESS WORLDWIDE",
                  "productCode": "P",
                  "localProductCode": "P",
                  "networkTypeCode": "TD",
                  "isCustomerAgreement": false,
                  "weight": { "volumetric": 0.6, "provided": 2.5, "unitOfMeasurement": "metric" },
                  "totalPrice": [
                    { "currencyType": "BILLC", "priceCurrency": "DKK", "price": 412.5 },
                    { "currencyType": "PULCL", "priceCurrency": "DKK", "price": 412.5 }
                  ],
                  "totalPriceBreakdown": [
                    {
                      "currencyType": "BILLC",
                      "priceCurrency": "DKK",
                      "priceBreakdown": [
                        { "typeCode": "SPRQT", "price": 330.0 },
                        { "typeCode": "STTXA", "price": 82.5 }
                      ]
                    },
                    {
                      "currencyType": "PULCL",
                      "priceCurrency": "DKK",
                      "priceBreakdown": [
                        { "typeCode": "SPRQT", "price": 999.0 },
                        { "typeCode": "STTXA", "price": 111.0 }
                      ]
                    }
                  ],
                  "detailedPriceBreakdown": [],
                  "deliveryCapabilities": {
                    "deliveryTypeCode": "QDDC",
                    "estimatedDeliveryDateAndTime": "2026-11-05T23:59:00",
                    "destinationServiceAreaCode": "BER",
                    "totalTransitDays": 3
                  }
                },
                {
                  "productName": "ECONOMY SELECT",
                  "productCode": "H",
                  "totalPrice": [
                    { "currencyType": "BILLC", "priceCurrency": "DKK", "price": 250.0 }
                  ],
                  "totalPriceBreakdown": [
                    {
                      "currencyType": "BILLC",
                      "priceCurrency": "DKK",
                      "priceBreakdown": [
                        { "typeCode": "SPRQT", "price": 200.0 },
                        { "typeCode": "STTXA", "price": 50.0 }
                      ]
                    }
                  ]
                }
              ],
              "exchangeRates": [
                { "currentExchangeRate": 1.0, "currency": "DKK", "baseCurrency": "DKK" }
              ]
            }
            """;

        public const string ErrorJson = """
            {
              "instance": "/expressapi/rates",
              "detail": "Unable to find any matching product",
              "title": "Not Found",
              "message": "No products found for the requested route",
              "status": "404"
            }
            """;
    }
}
