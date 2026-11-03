using System.ComponentModel.DataAnnotations;
using CompassionCloud.Models;
using Xunit;

namespace CompassionCloud.Tests
{
    /// <summary>Donation validation rules (what the donation form enforces on the server).</summary>
    [Trait("Category", "Unit")]
    public class DonationValidationTests
    {
        private static IList<ValidationResult> Validate(Donation donation)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(donation, new ValidationContext(donation), results, validateAllProperties: true);
            return results;
        }

        private static Donation Valid() => new()
        {
            DonorName = "Test Donor",
            DonorEmail = "donor@test.com",
            Amount = 100m,
            Currency = "ZAR",
            DonationType = "One-time"
        };

        [Fact]
        public void ValidDonation_PassesValidation() => Assert.Empty(Validate(Valid()));

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        [InlineData(0.5)]
        public void AmountBelowMinimum_FailsValidation(double amount)
        {
            var d = Valid();
            d.Amount = (decimal)amount;
            Assert.Contains(Validate(d), r => r.MemberNames.Contains("Amount"));
        }

        [Fact]
        public void UnsupportedCurrency_FailsValidation()
        {
            var d = Valid();
            d.Currency = "GBP";
            Assert.Contains(Validate(d), r => r.MemberNames.Contains("Currency"));
        }

        [Fact]
        public void InvalidEmail_FailsValidation()
        {
            var d = Valid();
            d.DonorEmail = "not-an-email";
            Assert.Contains(Validate(d), r => r.MemberNames.Contains("DonorEmail"));
        }

        [Fact]
        public void UnknownDonationType_FailsValidation()
        {
            var d = Valid();
            d.DonationType = "Weekly";
            Assert.Contains(Validate(d), r => r.MemberNames.Contains("DonationType"));
        }
    }
}
