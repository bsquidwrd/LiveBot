namespace LiveBot.Core.HealthCheck
{
    public class Program
    {
        private static readonly HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static async Task Main(string[] args)
        {
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "LiveBot Health Check");

                var targets = BuildCandidateUrls();

                foreach (var url in targets)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        using var response = await client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Healthcheck OK: {url} -> {(int)response.StatusCode}");
                            Environment.Exit(0);
                        }

                        Console.WriteLine($"Attempted {url}, got {(int)response.StatusCode}. Trying next...");
                    }
                    catch (HttpRequestException hre)
                    {
                        Console.WriteLine($"Connection error to {url}: {hre.Message}");
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine($"Timeout when connecting to {url}");
                    }
                }

                Console.WriteLine("Request did not return success status on any candidate URL");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception during healthcheck");
                Console.WriteLine($"Exception: {ex}");
                Environment.Exit(1);
            }
        }

        private static IEnumerable<string> BuildCandidateUrls()
        {
            // 1) Explicit override
            var explicitUrl = Environment.GetEnvironmentVariable("HEALTHCHECK_URL");
            if (!string.IsNullOrWhiteSpace(explicitUrl))
            {
                if (Uri.TryCreate(explicitUrl, UriKind.Absolute, out var full))
                {
                    yield return full.ToString();
                    yield break;
                }

                // Allow host:port without scheme
                if (Uri.TryCreate($"http://{explicitUrl}", UriKind.Absolute, out var fullHttp))
                {
                    yield return fullHttp.ToString();
                    yield break;
                }
            }

            // 2) Derive from ASPNETCORE_URLS or ASPNETCORE_HTTP_PORTS
            var appUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var httpPorts = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
            var port = FirstPortFromEnv(appUrls) ?? FirstPortFromEnv(httpPorts);

            // 3) Generic PORT env var (Heroku/Cloud Run style)
            port ??= FirstPortFromEnv(Environment.GetEnvironmentVariable("PORT"));

            // Build base URL on localhost using derived or default port
            var basePorts = port is not null
                ? new[] { port.Value }
                : new[] { 8080, 80, 5000 }; // common defaults

            // Optional custom path
            var pathOverride = Environment.GetEnvironmentVariable("HEALTHCHECK_PATH");
            foreach (var bp in basePorts.Distinct())
            {
                var baseUrl = new UriBuilder
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = bp,
                    Path = "/"
                }.Uri;

                if (!string.IsNullOrWhiteSpace(pathOverride))
                {
                    yield return Combine(baseUrl, pathOverride!);
                    continue;
                }

                // Try common health endpoints then root
                yield return Combine(baseUrl, "/healthcheck");
                yield return baseUrl.ToString();
            }
        }

        private static int? FirstPortFromEnv(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            // ASPNETCORE_URLS may be like: http://+:8080;https://+:8443
            var first = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(first)) return null;

            if (Uri.TryCreate(first, UriKind.Absolute, out var uri))
            {
                if (uri.Port > 0) return uri.Port;
            }

            // Handle plain port list like "8080;8443"
            if (int.TryParse(first, out var p)) return p;
            return null;
        }

        private static string Combine(Uri baseUri, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return baseUri.ToString();
            if (!path.StartsWith('/')) path = "/" + path;
            return new Uri(baseUri, path).ToString();
        }
    }
}
