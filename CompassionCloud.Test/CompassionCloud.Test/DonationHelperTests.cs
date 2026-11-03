using CompassionCloud.Helpers;
using Xunit;
using System.Collections.Generic;

namespace CompassionCloud.Tests
{
    [Trait("Category", "Unit")]
    public class DonationHelperTests
    {
        // Test 1: Certificate Formatting
        [Fact]
        public void FormatTaxCertificateNumber_ReturnsFormattedString()
        {
            string rawReference = "A1B2C3D4E5F6";
            var result = DonationHelper.FormatTaxCertificateNumber(rawReference);

            Assert.StartsWith("CC-", result);
            Assert.Contains("A1B2-C3D4-E5F6", result);
        }

        // Test 2: Total Calculation
        [Fact]
        public void CalculateDonationTotal_ReturnsCorrectSum()
        {
            var amounts = new List<decimal> { 100.50m, 250.00m, 50.00m };
            var result = DonationHelper.CalculateDonationTotal(amounts);

            Assert.Equal(400.50m, result);
        }

        // Test 3: Currency conversion
        [Fact]
        public void ConvertCurrency_ZarToUsd_ReturnsCorrectAmount()
        {
            // 1000 ZAR * 0.054 = 54 USD
            var result = DonationHelper.ConvertCurrency(1000m, "ZAR", "USD");
            Assert.Equal(54m, result);
        }

        // Test 4: Donation validation (unsupported currency is rejected)
        [Fact]
        public void ConvertCurrency_UnsupportedCurrency_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => DonationHelper.ConvertCurrency(100m, "ZAR", "XYZ"));
        }

        // Test 5: Total calculation handles missing input
        [Fact]
        public void CalculateDonationTotal_NullList_ReturnsZero()
        {
            Assert.Equal(0m, DonationHelper.CalculateDonationTotal(null!));
        }

        // Test 6: Certificate formatting handles empty input
        [Fact]
        public void FormatTaxCertificateNumber_EmptyInput_ReturnsPlaceholder()
        {
            Assert.Equal("CC-0000-XXXX-XXXX-XXXX", DonationHelper.FormatTaxCertificateNumber(""));
        }

        // Test 7: Donation amount formatting
        [Fact]
        public void FormatDonationAmount_Zar_UsesRandSymbol()
        {
            var result = DonationHelper.FormatDonationAmount(1500m, "ZAR");
            Assert.StartsWith("R ", result);
        }
    }
}
