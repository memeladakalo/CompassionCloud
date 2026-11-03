namespace CompassionCloud.Helpers
{
    /// <summary>
    /// Utility methods for the Compassion Cloud donation system.
    /// Shared between the web app and any other consuming project.
    /// </summary>
    public static class DonationHelper
    {
        /// <summary>
        /// Formats a tax certificate reference number into a standard readable format.
        /// Example input: "A1B2C3D4E5F6"
        /// Example output: "CC-2026-A1B2-C3D4-E5F6"
        /// </summary>
        public static string FormatTaxCertificateNumber(string rawReference)
        {
            if (string.IsNullOrWhiteSpace(rawReference))
                return "CC-0000-XXXX-XXXX-XXXX";

            string clean = rawReference.Replace("-", "").ToUpper();

            if (clean.Length < 12)
                clean = clean.PadRight(12, '0');

            string part1 = clean.Substring(0, 4);
            string part2 = clean.Substring(4, 4);
            string part3 = clean.Substring(8, 4);

            return $"CC-{DateTime.UtcNow.Year}-{part1}-{part2}-{part3}";
        }

        /// <summary>
        /// Calculates the total donation amount from a list of amounts.
        /// </summary>
        public static decimal CalculateDonationTotal(IEnumerable<decimal> amounts)
        {
            if (amounts == null) return 0m;
            return amounts.Sum();
        }

        /// <summary>
        /// Converts an amount between supported currencies using static prototype rates.
        /// NOTE: Prototype only — real system will use live exchange rates.
        /// </summary>
        public static decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "ZAR", 1.00m },
                { "USD", 0.054m },
                { "EUR", 0.050m }
            };

            if (!rates.ContainsKey(fromCurrency) || !rates.ContainsKey(toCurrency))
                throw new ArgumentException("Unsupported currency.");

            decimal amountInZar = amount / rates[fromCurrency];
            return Math.Round(amountInZar * rates[toCurrency], 2);
        }

        /// <summary>
        /// Formats a donation amount with the correct currency symbol and thousands separator.
        /// </summary>
        public static string FormatDonationAmount(decimal amount, string currency)
        {
            string symbol = currency?.ToUpper() switch
            {
                "ZAR" => "R",
                "USD" => "$",
                "EUR" => "€",
                _ => currency ?? ""
            };

            return $"{symbol} {amount:N2}";
        }

        /// <summary>
        /// Generates a unique reference number for a new donation.
        /// </summary>
        public static string GenerateDonationReference()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
    }
}