using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace CompassionCloud.Tests
{
    /// <summary>
    /// Small-scale load test: 5 concurrent virtual users, each making 10 rounds of requests
    /// against public pages. Runs from Visual Studio Test Explorer (no extra tooling needed).
    /// Needs the site running; set COMPASSION_BASE_URL to target Azure instead of localhost.
    /// Tagged Category=Load so the build pipeline skips it.
    /// </summary>
    [Trait("Category", "Load")]
    public class LoadTests
    {
        private const int ConcurrentUsers = 5;
        private const int RoundsPerUser = 10;
        private static readonly string[] Paths = { "/", "/Home/About", "/Donation", "/health" };

        private readonly ITestOutputHelper _output;
        public LoadTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task FiveConcurrentUsers_BrowseSite_NoFailuresAndAcceptableLatency()
        {
            var baseUrl = (Environment.GetEnvironmentVariable("COMPASSION_BASE_URL") ?? "http://localhost:5031").TrimEnd('/');

            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var timings = new System.Collections.Concurrent.ConcurrentBag<long>();
            var failures = 0;

            async Task VirtualUser()
            {
                using var client = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };
                for (var i = 0; i < RoundsPerUser; i++)
                {
                    foreach (var path in Paths)
                    {
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            var response = await client.GetAsync(baseUrl + path);
                            sw.Stop();
                            timings.Add(sw.ElapsedMilliseconds);
                            if (!response.IsSuccessStatusCode) Interlocked.Increment(ref failures);
                        }
                        catch
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                }
            }

            var total = Stopwatch.StartNew();
            await Task.WhenAll(Enumerable.Range(0, ConcurrentUsers).Select(_ => Task.Run(VirtualUser)));
            total.Stop();

            var sorted = timings.OrderBy(t => t).ToArray();
            var expected = ConcurrentUsers * RoundsPerUser * Paths.Length;
            var p95 = sorted.Length == 0 ? long.MaxValue : sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];

            _output.WriteLine($"Target:            {baseUrl}");
            _output.WriteLine($"Concurrent users:  {ConcurrentUsers}");
            _output.WriteLine($"Requests sent:     {expected}");
            _output.WriteLine($"Failures:          {failures}");
            _output.WriteLine($"Total time:        {total.ElapsedMilliseconds} ms");
            if (sorted.Length > 0)
            {
                _output.WriteLine($"Avg response:      {sorted.Average():F0} ms");
                _output.WriteLine($"Median response:   {sorted[sorted.Length / 2]} ms");
                _output.WriteLine($"95th percentile:   {p95} ms");
                _output.WriteLine($"Max response:      {sorted.Last()} ms");
            }

            Assert.Equal(0, failures);
            Assert.True(p95 < 3000, $"95th percentile {p95} ms exceeded 3000 ms");
        }
    }
}
